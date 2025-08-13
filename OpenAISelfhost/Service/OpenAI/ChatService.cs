using Azure;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI.Responses;
using OpenAISelfhost.DataContracts.Common.Chat;
using OpenAISelfhost.DataContracts.DataTables;
using OpenAISelfhost.DataContracts.Request.Chat;
using OpenAISelfhost.DataContracts.Response.Chat;
using OpenAISelfhost.Exceptions.Http;
using OpenAISelfhost.Service.Billing;
using OpenAISelfhost.Service.Interface;
using OpenAISelfhost.Service.MCP;
using OpenAISelfhost.Service.OpenAI.Utils;
using OpenAISelfhost.Transports;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatResponse = OpenAISelfhost.DataContracts.Response.Chat.ChatResponse;
using MsxChatMessage = Microsoft.Extensions.AI.ChatMessage;
using MsxChatRole = Microsoft.Extensions.AI.ChatRole;

namespace OpenAISelfhost.Service.OpenAI
{
    public class ChatService : IChatService
    {
        private readonly IUserService userService;
        private readonly ITransactionService transactionService;
        private readonly IMCPTransportService mcpRemoteTransportService;

        public ChatService(IUserService userService, ITransactionService transactionService, IMCPTransportService mcpRemoteTransportService)
        {
            this.userService = userService;
            this.transactionService = transactionService;
            this.mcpRemoteTransportService = mcpRemoteTransportService;
        }

        public async Task<ChatResponse> RequestCompletion(ChatModel model, ChatCompletionRequest request, int userId)
        {
            var user = userService.GetUser(userId);
            if (user == null)
                throw new AuthorizationException("Unable to find user for billing");
            if (user.RemainingCredit <= 0)
                throw new InsufficientTokenException("You don't have enough token to execute this request");
            try
            {
                var result = await (model.IsVision switch
                {
                    true => RequestCompletionGPT4Vision(model, request, userId),
                    false => model.ReasoningModel
                        ? RequestCompletionWithAzureChatClient(model, request, userId)
                        : RequestCompletionWithSDK(model, request, userId),
                });
                var cost = result.PromptTokens * model.CostPromptToken + result.ResponseTokens * model.CostResponseToken;
                user.RemainingCredit -= cost;
                userService.UpdateUser(user);
                transactionService.RecordTransaction(userId, result.Id, result.PromptTokens, result.ResponseTokens, result.TotalTokens, model.Identifier, cost);
                return result;
            }
            catch (Exception e)
            {
                throw new ChatExecutionException($"Error raised when executing chat request: {e.Message}");
            }
        }

        public async IAsyncEnumerable<PartialChatResponse> RequestStreamingCompletion(ChatModel model, ChatCompletionRequest request, int userId)
        {
            var user = userService.GetUser(userId);
            if (user == null)
                throw new AuthorizationException("Unable to find user for billing");
            if (user.RemainingCredit <= 0)
                throw new InsufficientTokenException("You don't have enough token to execute this request");

            var chatClient = GetChatClient(model)
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();
            var inputTokenCount = 0;
            var outputTokenCount = 0;
            var resultId = Guid.NewGuid().ToString();

            var tools = Enumerable.Empty<McpClientTool>();
            var toolsFromRemote = Enumerable.Empty<McpClientTool>();
            RemoteMcpTransport? remoteMcpTransport = null;
            LocalMCPService? localMcpService = null;

            // Only load tools if model supports them
            if (model.SupportTool)
            {
                // remote mcp
                if (!string.IsNullOrEmpty(request.MCPCorrelationId))
                {
                    remoteMcpTransport = mcpRemoteTransportService.GetTransport(request.MCPCorrelationId);
                }
                var pipe = new MCPPipe();
                localMcpService = new LocalMCPService(pipe);
                _ = localMcpService.StartAsync();

                try
                {
                    var clientTransport = new StreamClientTransport(pipe.ClientToServerPipe.Writer.AsStream(), pipe.ServerToClientPipe.Reader.AsStream());
                    var mcpClient = await McpClientFactory.CreateAsync(clientTransport);
                    tools = await mcpClient.ListToolsAsync();
                }
                catch (Exception) { }

                if (remoteMcpTransport != null)
                {
                    bool loaded = false;
                    try
                    {
                        var remoteTransport = new StreamClientTransport(
                        remoteMcpTransport.ClientToServerPipe.Writer.AsStream(),
                        remoteMcpTransport.ServerToClientPipe.Reader.AsStream());
                        var mcpRemoteClient = await McpClientFactory.CreateAsync(remoteTransport);
                        toolsFromRemote = await mcpRemoteClient.ListToolsAsync();
                        loaded = true;
                    }
                    catch (Exception)
                    {
                    }
                    if (!loaded)
                    {
                        yield return new PartialChatResponse()
                        {
                            Data = "",
                            IsEnd = false,
                            FinishReason = "error_remote_mcp",
                        };
                    }
                }
            }

            var response = chatClient.GetStreamingResponseAsync(ToChatMessages(request), new()
            {
                Tools = [.. tools, .. toolsFromRemote],
                AdditionalProperties = new()
                {
                    ["stream_options"] = new
                    {
                        include_usage = true
                    }
                }
            });

            Microsoft.Extensions.AI.ChatFinishReason? lastReason = null;
            await foreach (var update in response)
            {
                // usage data
                foreach (var usageContent in update.Contents.OfType<UsageContent>())
                {
                    inputTokenCount += (int)(usageContent.Details.InputTokenCount ?? 0);
                    outputTokenCount += (int)(usageContent.Details.OutputTokenCount ?? 0);
                }
                if (lastReason == Microsoft.Extensions.AI.ChatFinishReason.Stop)
                {
                    continue;
                }
                foreach (var functionCall in update.Contents.OfType<FunctionCallContent>())
                {
                    yield return new PartialChatResponse()
                    {
                        Data = "",
                        IsEnd = false,
                        FinishReason = "function_call",
                        ToolName = functionCall.Name,
                        ToolParameters = JsonSerializer.Serialize(functionCall.Arguments, new JsonSerializerOptions()
                        {
                            Converters =
                            {
                                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                            }
                        }),
                    };
                }
                lastReason = update.FinishReason;
                yield return new PartialChatResponse()
                {
                    Data = update.Text,
                    IsEnd = update.FinishReason == Microsoft.Extensions.AI.ChatFinishReason.Stop,
                    FinishReason = update.FinishReason.ToString() ?? "N/A",
                };
            }
            var cost = inputTokenCount * model.CostPromptToken + outputTokenCount * model.CostResponseToken;
            user.RemainingCredit -= cost;
            userService.UpdateUser(user);
            transactionService.RecordTransaction(userId, resultId, inputTokenCount, outputTokenCount, inputTokenCount + outputTokenCount, model.Identifier, cost);

            // Dispose remote transport after streaming is complete
            if (remoteMcpTransport != null)
            {
                remoteMcpTransport.Dispose();
            }

            // Dispose local MCP service after streaming is complete
            if (localMcpService != null)
            {
                localMcpService.Dispose();
            }

            if (lastReason != Microsoft.Extensions.AI.ChatFinishReason.Stop)
            {
                yield return new PartialChatResponse()
                {
                    Data = "",
                    IsEnd = true,
                    FinishReason = lastReason?.ToString() ?? "N/A",
                };
            }
        }

        private IChatClient GetChatClient(ChatModel model)
        {
            if (model.ReasoningModel)
            {
                var azureClient = new AzureOpenAIClient(new Uri(model.Endpoint), new AzureKeyCredential(model.Key));
                return azureClient.GetChatClient(model.Deployment).AsIChatClient();
            }
            var aiInferenceOptions = new AzureAIInferenceClientOptions();
            var field = typeof(AzureAIInferenceClientOptions).GetField("<Version>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            // Only set the API version if explicitly provided
            if (!string.IsNullOrEmpty(model.ApiVersionOverride))
            {
                field?.SetValue(aiInferenceOptions, model.ApiVersionOverride);
            }
            return new ChatCompletionsClient(
                new Uri(model.Endpoint),
                new AzureKeyCredential(model.Key),
                aiInferenceOptions
            ).AsIChatClient(model.Deployment);
        }

        private async Task<ChatResponse> RequestCompletionGPT4Vision(ChatModel model, ChatCompletionRequest request, int userId)
        {
            request.MaxTokens = model.MaxTokens;
            request.Stream = false;
            var gpt4VisionClient = new GPT4VisionClient(model.Endpoint, model.Deployment, model.Key);
            var result = await gpt4VisionClient.RequestCompletion(request);
            //generate random uuid
            result.Id = Guid.NewGuid().ToString();

            return result;
        }

        private async Task<ChatResponse> RequestCompletionWithSDK(ChatModel model, ChatCompletionRequest request, int userId)
        {
            var openAIClient = new AzureOpenAIClient(new Uri(model.Endpoint), new AzureKeyCredential(model.Key));
            var openAIChatClient = openAIClient.GetChatClient(model.Deployment);
            var chatClientBuilder = openAIChatClient.AsIChatClient()
                .AsBuilder();
            
            // Only enable function invocation if model supports tools
            if (model.SupportTool)
            {
                chatClientBuilder = chatClientBuilder.UseFunctionInvocation();
            }
            
            var chatClient = chatClientBuilder.Build();
            var response = await chatClient.GetResponseAsync(ToChatMessages(request));

            var result = new ChatResponse()
            {
                Message = response.Text,
                PromptTokens = (int)(response.Usage?.InputTokenCount ?? 0),
                ResponseTokens = (int)(response.Usage?.OutputTokenCount ?? 0),
                TotalTokens = (int)(response.Usage?.TotalTokenCount ?? 0),
                StopReason = response.FinishReason.ToString() ?? "N/A",
            };

            result.Id = Guid.NewGuid().ToString();
            return result;
        }

        private async Task<ChatResponse> RequestCompletionWithAzureChatClient(ChatModel model, ChatCompletionRequest request, int userId)
        {
            var azureClient = new AzureOpenAIClient(new Uri(model.Endpoint), new AzureKeyCredential(model.Key));
            global::OpenAI.Chat.ChatClient chatClient = azureClient.GetChatClient(model.Deployment);

            // Convert our request into Azure Chat messages
            List<global::OpenAI.Chat.ChatMessage> messages = ToAzureChatMessages(request).ToList();

            // For non-streaming completion, we can support basic tool calling via MCP
            // But the exact implementation would depend on the Azure ChatClient API capabilities
            var response = await chatClient.CompleteChatAsync(messages);

            var text = response.Value?.Content?.Count > 0 ? response.Value.Content[0].Text : string.Empty;
            var usage = response.Value?.Usage;

            var result = new ChatResponse()
            {
                Message = text,
                PromptTokens = (int)(usage?.InputTokenCount ?? 0),
                ResponseTokens = (int)(usage?.OutputTokenCount ?? 0),
                TotalTokens = (int)(usage?.TotalTokenCount ?? 0),
                StopReason = response.Value != null ? response.Value.FinishReason.ToString() : "N/A",
            };

            result.Id = Guid.NewGuid().ToString();
            return result;
        }

        private IEnumerable<MsxChatMessage> ToChatMessages(ChatCompletionRequest request)
        {
            foreach (var message in request.Messages)
            {
                var aiContent = message.Content.Select<ChatContentItem, AIContent>(c => c.Type switch
                {
                    ChatContentType.Text => new TextContent(c.Text),
                    ChatContentType.Image => ToBlobData(c.Base64Data!),
                    ChatContentType.Audio => ToBlobData(c.Base64Data!),
                    _ => throw new Exception("Invalid chat content type"),
                }).ToList();
                yield return message.Role switch
                {
                    ConversationRole.User => new MsxChatMessage(MsxChatRole.User, aiContent),
                    ConversationRole.Assistant => new MsxChatMessage(MsxChatRole.Assistant, aiContent),
                    ConversationRole.System => new MsxChatMessage(MsxChatRole.System, aiContent),
                    _ => throw new Exception("Invalid chat role"),
                };
            }
        }

        private IEnumerable<global::OpenAI.Chat.ChatMessage> ToAzureChatMessages(ChatCompletionRequest request)
        {
            foreach (var message in request.Messages)
            {
                // Merge all text content into a single string; ignore non-text types for reasoning models
                var text = string.Join(string.Empty, message.Content
                    .Where(c => c.Type == ChatContentType.Text)
                    .Select(c => c.Text));

                yield return message.Role switch
                {
                    ConversationRole.User => new global::OpenAI.Chat.UserChatMessage(text),
                    ConversationRole.Assistant => new global::OpenAI.Chat.AssistantChatMessage(text),
                    ConversationRole.System => new global::OpenAI.Chat.SystemChatMessage(text),
                    _ => throw new Exception("Invalid chat role"),
                };
            }
        }

        private static DataContent ToBlobData(string base64)
        {
            var mimeType = base64.Split(',')[0].Split(':')[1].Split(';')[0];
            var data = base64.Split(',')[1];
            return new DataContent(Convert.FromBase64String(data), mimeType);
        }
    }
}
