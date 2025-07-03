using Azure;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
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
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatResponse = OpenAISelfhost.DataContracts.Response.Chat.ChatResponse;
using ChatRole = Microsoft.Extensions.AI.ChatRole;

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
                    false => RequestCompletionWithSDK(model, request, userId),
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
            var openAIClient = new AzureOpenAIClient(new Uri(model.Endpoint), new AzureKeyCredential(model.Key));
            var azureInferenceClient = new ChatCompletionsClient(
                new Uri(model.Endpoint),
                new AzureKeyCredential(model.Key),
                new AzureAIInferenceClientOptions()
            );
            var chatClient = azureInferenceClient.AsIChatClient(model.Deployment)
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();
            var inputTokenCount = 0;
            var outputTokenCount = 0;
            var resultId = Guid.NewGuid().ToString();
            // remote mcp
            RemoteMcpTransport? remoteMcpTransport = null;
            if (!string.IsNullOrEmpty(request.MCPCorrelationId))
            {
                remoteMcpTransport = mcpRemoteTransportService.GetTransport(request.MCPCorrelationId);
            }
            var pipe = new MCPPipe();
            using var localMcpService = new LocalMCPService(pipe);
            _ = localMcpService.StartAsync();
            var tools = Enumerable.Empty<McpClientTool>();
            try
            {
                var clientTransport = new StreamClientTransport(pipe.ClientToServerPipe.Writer.AsStream(), pipe.ServerToClientPipe.Reader.AsStream());
                var mcpClient = await McpClientFactory.CreateAsync(clientTransport);
                tools = await mcpClient.ListToolsAsync();
            }
            catch (Exception) { }
            
            var toolsFromRemote = Enumerable.Empty<McpClientTool>();
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
            var response = chatClient.GetStreamingResponseAsync(ToChatMessages(request), new()
            {
                Tools = [.. tools, .. toolsFromRemote],
            });
            
            ChatFinishReason? lastReason = null;
            await foreach (var update in response)
            {
                // usage data
                foreach (var usageContent in update.Contents.OfType<UsageContent>())
                {
                    inputTokenCount += (int)(usageContent.Details.InputTokenCount ?? 0);
                    outputTokenCount += (int)(usageContent.Details.OutputTokenCount ?? 0);
                }
                if (lastReason == ChatFinishReason.Stop)
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
                    IsEnd = update.FinishReason == ChatFinishReason.Stop,
                    FinishReason = update.FinishReason.ToString() ?? "N/A",
                };
            }
            var cost = inputTokenCount * model.CostPromptToken + outputTokenCount * model.CostResponseToken;
            user.RemainingCredit -= cost;
            userService.UpdateUser(user);
            transactionService.RecordTransaction(userId, resultId, inputTokenCount, outputTokenCount, inputTokenCount + outputTokenCount, model.Identifier, cost);
            if (remoteMcpTransport != null)
            {
                remoteMcpTransport.Dispose();
            }
            if (lastReason != ChatFinishReason.Stop)
            {
                yield return new PartialChatResponse()
                {
                    Data = "",
                    IsEnd = true,
                    FinishReason = lastReason?.ToString() ?? "N/A",
                };
            }
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
            var chatClient = openAIChatClient.AsIChatClient()
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();
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

        private IEnumerable<ChatMessage> ToChatMessages(ChatCompletionRequest request)
        {
            foreach (var message in request.Messages)
            {
                var aiContent = message.Content.Select<ChatContentItem, AIContent>(c => c.Type switch
                {
                    ChatContentType.Text => new TextContent(c.Text),
                    ChatContentType.Image => new UriContent(c.ImageUrl!, "image"),
                    _ => throw new Exception("Invalid chat content type"),
                }).ToList();
                yield return message.Role switch
                {
                    ConversationRole.User => new ChatMessage(ChatRole.User, aiContent),
                    ConversationRole.Assistant => new ChatMessage(ChatRole.Assistant, aiContent),
                    ConversationRole.System => new ChatMessage(ChatRole.System, aiContent),
                    _ => throw new Exception("Invalid chat role"),
                };
            }
        }
    }
}
