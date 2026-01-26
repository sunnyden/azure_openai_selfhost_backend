using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAISelfhost.DataContracts.Common.Chat;
using OpenAISelfhost.DataContracts.Enums;
using OpenAISelfhost.DataContracts.Request.ChatHistory;
using OpenAISelfhost.DataContracts.Response.ChatHistory;
using OpenAISelfhost.DataContracts.Response.Common;
using OpenAISelfhost.Service.ChatHistory;
using System.Text.Json;

namespace OpenAISelfhost.Controllers
{
    [ApiController]
    [Route("chat-history")]
    public class ChatHistoryController : ApiControllerBase
    {
        private readonly IChatHistoryService chatHistoryService;
        private readonly JsonSerializerOptions jsonOptions;

        public ChatHistoryController(IChatHistoryService chatHistoryService)
        {
            this.chatHistoryService = chatHistoryService;
            this.jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        [HttpPost("create")]
        [Authorize]
        public ApiResponse<ChatHistoryResponse> CreateChatHistory([FromBody] CreateChatHistoryRequest request)
        {
            var userId = GetUserId();

            var chatHistory = chatHistoryService.CreateChatHistory(userId, request.Title, request.Messages);

            var messages = JsonSerializer.Deserialize<List<ChatMessage>>(
                chatHistory.Messages ?? "[]", jsonOptions) ?? new List<ChatMessage>();

            return new ApiResponse<ChatHistoryResponse>
            {
                Data = new ChatHistoryResponse
                {
                    Id = chatHistory.Id,
                    Title = chatHistory.Title,
                    Messages = messages,
                    CreatedAt = chatHistory.CreatedAt,
                    UpdatedAt = chatHistory.UpdatedAt
                }
            };
        }

        [HttpGet("get/{id}")]
        [Authorize]
        public ApiResponse<ChatHistoryResponse> GetChatHistory(string id)
        {
            var userId = GetUserId();

            var chatHistory = chatHistoryService.GetChatHistory(id, userId);
            if (chatHistory == null)
            {
                throw new Exceptions.Http.ChatHistoryNotFoundException($"Chat history with id {id} not found");
            }

            var messages = JsonSerializer.Deserialize<List<ChatMessage>>(
                chatHistory.Messages ?? "[]", jsonOptions) ?? new List<ChatMessage>();

            return new ApiResponse<ChatHistoryResponse>
            {
                Data = new ChatHistoryResponse
                {
                    Id = chatHistory.Id,
                    Title = chatHistory.Title,
                    Messages = messages,
                    CreatedAt = chatHistory.CreatedAt,
                    UpdatedAt = chatHistory.UpdatedAt
                }
            };
        }

        [HttpGet("list")]
        [Authorize]
        public ApiResponse<IEnumerable<ChatHistoryListResponse>> ListChatHistories([FromQuery] int? userId = null)
        {
            var isAdmin = User.IsInRole(UserType.Admin);
            var targetUserId = (isAdmin && userId.HasValue) ? userId.Value : GetUserId();

            var chatHistories = chatHistoryService.GetChatHistoriesForUser(targetUserId);

            var listResponse = chatHistories.Select(ch =>
            {
                var messages = JsonSerializer.Deserialize<List<ChatMessage>>(
                    ch.Messages ?? "[]", jsonOptions) ?? new List<ChatMessage>();

                return new ChatHistoryListResponse
                {
                    Id = ch.Id,
                    Title = ch.Title,
                    CreatedAt = ch.CreatedAt,
                    UpdatedAt = ch.UpdatedAt,
                    MessageCount = messages.Count
                };
            });

            return new ApiResponse<IEnumerable<ChatHistoryListResponse>>
            {
                Data = listResponse
            };
        }

        [HttpGet("all")]
        [Authorize(Roles = UserType.Admin)]
        public ApiResponse<IEnumerable<ChatHistoryListResponse>> ListAllChatHistories()
        {
            var chatHistories = chatHistoryService.GetAllChatHistories();

            var listResponse = chatHistories.Select(ch =>
            {
                var messages = JsonSerializer.Deserialize<List<ChatMessage>>(
                    ch.Messages ?? "[]", jsonOptions) ?? new List<ChatMessage>();

                return new ChatHistoryListResponse
                {
                    Id = ch.Id,
                    Title = ch.Title,
                    CreatedAt = ch.CreatedAt,
                    UpdatedAt = ch.UpdatedAt,
                    MessageCount = messages.Count
                };
            });

            return new ApiResponse<IEnumerable<ChatHistoryListResponse>>
            {
                Data = listResponse
            };
        }

        [HttpPost("update-title")]
        [Authorize]
        public ApiResponse<ChatHistoryResponse> UpdateChatTitle([FromBody] UpdateChatHistoryTitleRequest request)
        {
            var userId = GetUserId();

            chatHistoryService.UpdateChatHistoryTitle(request.Id, userId, request.Title);

            var chatHistory = chatHistoryService.GetChatHistory(request.Id, userId);
            if (chatHistory == null)
            {
                throw new Exceptions.Http.ChatHistoryNotFoundException($"Chat history with id {request.Id} not found");
            }

            var messages = JsonSerializer.Deserialize<List<ChatMessage>>(
                chatHistory.Messages ?? "[]", jsonOptions) ?? new List<ChatMessage>();

            return new ApiResponse<ChatHistoryResponse>
            {
                Data = new ChatHistoryResponse
                {
                    Id = chatHistory.Id,
                    Title = chatHistory.Title,
                    Messages = messages,
                    CreatedAt = chatHistory.CreatedAt,
                    UpdatedAt = chatHistory.UpdatedAt
                }
            };
        }

        [HttpPost("update")]
        [Authorize]
        public ApiResponse<ChatHistoryResponse> UpdateChatHistory([FromBody] UpdateChatHistoryRequest request)
        {
            var userId = GetUserId();

            chatHistoryService.UpdateChatHistory(request.Id, userId, request.Title, request.Messages);

            var chatHistory = chatHistoryService.GetChatHistory(request.Id, userId);
            if (chatHistory == null)
            {
                throw new Exceptions.Http.ChatHistoryNotFoundException($"Chat history with id {request.Id} not found");
            }

            var messages = JsonSerializer.Deserialize<List<ChatMessage>>(
                chatHistory.Messages ?? "[]", jsonOptions) ?? new List<ChatMessage>();

            return new ApiResponse<ChatHistoryResponse>
            {
                Data = new ChatHistoryResponse
                {
                    Id = chatHistory.Id,
                    Title = chatHistory.Title,
                    Messages = messages,
                    CreatedAt = chatHistory.CreatedAt,
                    UpdatedAt = chatHistory.UpdatedAt
                }
            };
        }

        [HttpPost("append-messages")]
        [Authorize]
        public ApiResponse<ChatHistoryResponse> AppendMessages([FromBody] AppendMessagesRequest request)
        {
            var userId = GetUserId();

            chatHistoryService.AppendMessages(request.Id, userId, request.Messages);

            var chatHistory = chatHistoryService.GetChatHistory(request.Id, userId);
            if (chatHistory == null)
            {
                throw new Exceptions.Http.ChatHistoryNotFoundException($"Chat history with id {request.Id} not found");
            }

            var messages = JsonSerializer.Deserialize<List<ChatMessage>>(
                chatHistory.Messages ?? "[]", jsonOptions) ?? new List<ChatMessage>();

            return new ApiResponse<ChatHistoryResponse>
            {
                Data = new ChatHistoryResponse
                {
                    Id = chatHistory.Id,
                    Title = chatHistory.Title,
                    Messages = messages,
                    CreatedAt = chatHistory.CreatedAt,
                    UpdatedAt = chatHistory.UpdatedAt
                }
            };
        }

        [HttpPost("delete")]
        [Authorize]
        public ApiResponse<string> DeleteChatHistory([FromBody] DeleteChatHistoryRequest request)
        {
            var userId = GetUserId();

            chatHistoryService.DeleteChatHistory(request.Id, userId);

            return new ApiResponse<string>
            {
                Data = $"Chat history {request.Id} deleted successfully"
            };
        }

        [HttpPost("delete-all")]
        [Authorize(Roles = UserType.Admin)]
        public ApiResponse<string> DeleteAllUserChats([FromBody] DeleteAllUserChatsRequest request)
        {
            var chatHistories = chatHistoryService.GetChatHistoriesForUser(request.UserId);
            var count = chatHistories.Count();

            chatHistoryService.DeleteAllChatHistoriesForUser(request.UserId);

            return new ApiResponse<string>
            {
                Data = $"Deleted {count} chat histories for user {request.UserId}"
            };
        }
    }
}
