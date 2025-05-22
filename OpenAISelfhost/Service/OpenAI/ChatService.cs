using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using OpenAISelfhost.DataContracts.Common.Chat;
using OpenAISelfhost.DataContracts.DataTables;
using OpenAISelfhost.DataContracts.Request.Chat;
using OpenAISelfhost.DataContracts.Response.Chat;
using OpenAISelfhost.Exceptions.Http;
using OpenAISelfhost.Service.Billing;
using OpenAISelfhost.Service.Interface;
using OpenAISelfhost.Service.OpenAI.Utils;
using ChatMessage = OpenAI.Chat.ChatMessage;

namespace OpenAISelfhost.Service.OpenAI
{
    public class ChatService : IChatService
    {
        private readonly IUserService userService;
        private readonly ITransactionService transactionService;

        public ChatService(IUserService userService, ITransactionService transactionService)
        {
            this.userService = userService;
            this.transactionService = transactionService;
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
            var openAIClient = new AzureOpenAIClient(new Uri(model.Endpoint), new AzureKeyCredential(model.Key));
            var chatClient = openAIClient.GetChatClient(model.Deployment);
            var response = chatClient.CompleteChatStreamingAsync(ToChatMessages(request));
            var inputTokenCount = 0;
            var outputTokenCount = 0;
            var resultId = Guid.NewGuid().ToString();
            ChatFinishReason? lastReason = null;
            await foreach (var chunk in response)
            {
                lastReason = chunk.FinishReason;
                // usage data
                var usage = chunk.Usage;
                inputTokenCount += usage.InputTokenCount;
                outputTokenCount += usage.OutputTokenCount;
                yield return new PartialChatResponse()
                {
                    Data = string.Join("\n", chunk.ContentUpdate.Select(c => c.Text)),
                    IsEnd = false,
                    FinishReason = chunk.FinishReason.ToString() ?? "N/A",
                };
            }
            var cost = inputTokenCount * model.CostPromptToken + outputTokenCount * model.CostResponseToken;
            transactionService.RecordTransaction(userId, resultId, inputTokenCount, outputTokenCount, inputTokenCount + outputTokenCount, model.Identifier, cost);

            yield return new PartialChatResponse()
            {
                Data = "",
                IsEnd = true,
                FinishReason = lastReason?.ToString() ?? "N/A",
            };
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
            var chatClient = openAIClient.GetChatClient(model.Deployment);
            var response = await chatClient.CompleteChatAsync(ToChatMessages(request));

            var result = new ChatResponse()
            {
                Message = response.Value.Content[0].Text,
                PromptTokens = response.Value.Usage.InputTokenCount,
                ResponseTokens = response.Value.Usage.OutputTokenCount,
                TotalTokens = response.Value.Usage.TotalTokenCount,
                StopReason = response.Value.FinishReason.ToString() ?? "N/A",
            };

            result.Id = Guid.NewGuid().ToString();
            return result;
        }

        private IEnumerable<ChatMessage> ToChatMessages(ChatCompletionRequest request)
        {
            foreach (var message in request.Messages)
            {
                var content = string.Join("\n", message.Content.Select(c => c.Text));
                yield return message.Role switch
                {
                    ChatRole.User => new UserChatMessage(content),
                    ChatRole.Assistant => new AssistantChatMessage(content),
                    ChatRole.System => new SystemChatMessage(content),
                    _ => throw new Exception("Invalid chat role"),
                };
            }
        }
    }
}
