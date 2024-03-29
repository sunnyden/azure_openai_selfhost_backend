﻿using Azure;
using Azure.AI.OpenAI;
using OpenAISelfhost.DataContracts.Common.Chat;
using OpenAISelfhost.DataContracts.DataTables;
using OpenAISelfhost.DataContracts.Request.Chat;
using OpenAISelfhost.DataContracts.Response.Chat;
using OpenAISelfhost.Exceptions.Http;
using OpenAISelfhost.Service.Billing;
using OpenAISelfhost.Service.Interface;
using OpenAISelfhost.Service.OpenAI.Utils;

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

        private async Task<ChatResponse> RequestCompletionGPT4Vision(ChatModel model,ChatCompletionRequest request, int userId)
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
            var chatCompletionOptions = new ChatCompletionsOptions()
            {
                DeploymentName = model.Deployment,
                MaxTokens = model.MaxTokens,
            };

            request.Messages.Select(ToChatMessage).ToList().ForEach(chatCompletionOptions.Messages.Add);
            var openAIClient = new OpenAIClient(new Uri(model.Endpoint), new AzureKeyCredential(model.Key));
            Response<ChatCompletions> response = await openAIClient.GetChatCompletionsAsync(chatCompletionOptions);

            var result = new ChatResponse()
            {
                Message = response.Value.Choices[0].Message.Content,
                PromptTokens = response.Value.Usage.PromptTokens,
                ResponseTokens = response.Value.Usage.CompletionTokens,
                TotalTokens = response.Value.Usage.TotalTokens,
                StopReason = response.Value.Choices[0].FinishReason?.ToString() ?? "N/A",
            };

            result.Id = Guid.NewGuid().ToString();
            return result;

            ChatRequestMessage ToChatMessage(ChatMessage message)
            {
                switch (message.Role)
                {
                    case DataContracts.Common.Chat.ChatRole.User:
                        return new ChatRequestUserMessage(string.Join("\n", message.Content.Select(c => c.Text)));
                    case DataContracts.Common.Chat.ChatRole.Assistant:
                        return new ChatRequestAssistantMessage(string.Join("\n", message.Content.Select(c => c.Text)));
                    case DataContracts.Common.Chat.ChatRole.System:
                        return new ChatRequestSystemMessage(string.Join("\n", message.Content.Select(c => c.Text)));
                    default:
                        throw new Exception("Invalid chat role");
                }
            }
        }
    }
}
