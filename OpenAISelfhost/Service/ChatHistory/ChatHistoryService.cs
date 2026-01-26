using Microsoft.EntityFrameworkCore;
using OpenAISelfhost.DatabaseContext;
using OpenAISelfhost.DataContracts.Common.Chat;
using OpenAISelfhost.Exceptions.Http;
using OpenAISelfhost.Service.Interface;
using System.Text.Json;

namespace OpenAISelfhost.Service.ChatHistory
{
    public class ChatHistoryService : IChatHistoryService
    {
        private readonly ServiceDatabaseContext databaseContext;
        private readonly IUserService userService;
        private readonly JsonSerializerOptions jsonOptions;

        public ChatHistoryService(ServiceDatabaseContext databaseContext, IUserService userService)
        {
            this.databaseContext = databaseContext;
            this.userService = userService;
            this.jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        // Create
        public DataContracts.DataTables.ChatHistory CreateChatHistory(int userId, string? title, List<ChatMessage> messages)
        {
            // Validation
            if (!userService.UserExists(userId))
            {
                throw new UserNotFoundException($"User with id {userId} not found");
            }

            if (messages == null || messages.Count == 0)
            {
                throw new InvalidPayloadException("Messages cannot be null or empty");
            }

            if (title != null && title.Length > 145)
            {
                throw new InvalidPayloadException("Title exceeds maximum length of 145 characters");
            }

            // Generate ID
            var id = GenerateChatHistoryId();

            // Serialize messages
            var messagesJson = JsonSerializer.Serialize(messages, jsonOptions);

            // Create entity
            var chatHistory = new DataContracts.DataTables.ChatHistory
            {
                Id = id,
                UserId = userId,
                Title = title,
                Messages = messagesJson,
                UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            try
            {
                databaseContext.ChatHistories.Add(chatHistory);
                databaseContext.SaveChanges();

                // Reload to get the CreatedAt timestamp
                return GetChatHistory(id, userId) ?? chatHistory;
            }
            catch (DbUpdateException e)
            {
                throw new UnexpectedDatabaseExcception($"Unexpected database execution error: {e.Message}");
            }
        }

        // Read
        public DataContracts.DataTables.ChatHistory? GetChatHistory(string id, int userId)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidPayloadException("Chat history ID is required");
            }

            return databaseContext.ChatHistories
                .FirstOrDefault(ch => ch.Id == id && ch.UserId == userId);
        }

        public IEnumerable<DataContracts.DataTables.ChatHistory> GetChatHistoriesForUser(int userId)
        {
            return databaseContext.ChatHistories
                .AsNoTracking()
                .Where(ch => ch.UserId == userId)
                .OrderByDescending(ch => ch.CreatedAt)
                .ToList();
        }

        public IEnumerable<DataContracts.DataTables.ChatHistory> GetAllChatHistories()
        {
            return databaseContext.ChatHistories
                .AsNoTracking()
                .OrderByDescending(ch => ch.CreatedAt)
                .ToList();
        }

        // Update
        public void UpdateChatHistoryTitle(string id, int userId, string newTitle)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidPayloadException("Chat history ID is required");
            }

            if (newTitle != null && newTitle.Length > 145)
            {
                throw new InvalidPayloadException("Title exceeds maximum length of 145 characters");
            }

            var chatHistory = GetChatHistory(id, userId);
            if (chatHistory == null)
            {
                throw new ChatHistoryNotFoundException($"Chat history with id {id} not found");
            }

            chatHistory.Title = newTitle;
            chatHistory.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            try
            {
                databaseContext.ChatHistories.Update(chatHistory);
                databaseContext.SaveChanges();
            }
            catch (DbUpdateException e)
            {
                throw new UnexpectedDatabaseExcception($"Unexpected database execution error: {e.Message}");
            }
        }

        public void UpdateChatHistory(string id, int userId, string? title, List<ChatMessage> messages)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidPayloadException("Chat history ID is required");
            }

            if (messages == null || messages.Count == 0)
            {
                throw new InvalidPayloadException("Messages cannot be null or empty");
            }

            if (title != null && title.Length > 145)
            {
                throw new InvalidPayloadException("Title exceeds maximum length of 145 characters");
            }

            var chatHistory = GetChatHistory(id, userId);
            if (chatHistory == null)
            {
                throw new ChatHistoryNotFoundException($"Chat history with id {id} not found");
            }

            chatHistory.Title = title;
            chatHistory.Messages = JsonSerializer.Serialize(messages, jsonOptions);
            chatHistory.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            try
            {
                databaseContext.ChatHistories.Update(chatHistory);
                databaseContext.SaveChanges();
            }
            catch (DbUpdateException e)
            {
                throw new UnexpectedDatabaseExcception($"Unexpected database execution error: {e.Message}");
            }
        }

        public void AppendMessages(string id, int userId, List<ChatMessage> newMessages)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidPayloadException("Chat history ID is required");
            }

            if (newMessages == null || newMessages.Count == 0)
            {
                throw new InvalidPayloadException("Messages cannot be null or empty");
            }

            var chatHistory = GetChatHistory(id, userId);
            if (chatHistory == null)
            {
                throw new ChatHistoryNotFoundException($"Chat history with id {id} not found");
            }

            // Deserialize existing messages
            var existingMessages = JsonSerializer.Deserialize<List<ChatMessage>>(
                chatHistory.Messages ?? "[]", jsonOptions) ?? new List<ChatMessage>();

            // Append new messages
            existingMessages.AddRange(newMessages);

            // Serialize and update
            chatHistory.Messages = JsonSerializer.Serialize(existingMessages, jsonOptions);
            chatHistory.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            try
            {
                databaseContext.ChatHistories.Update(chatHistory);
                databaseContext.SaveChanges();
            }
            catch (DbUpdateException e)
            {
                throw new UnexpectedDatabaseExcception($"Unexpected database execution error: {e.Message}");
            }
        }

        // Delete
        public void DeleteChatHistory(string id, int userId)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidPayloadException("Chat history ID is required");
            }

            var chatHistory = GetChatHistory(id, userId);
            if (chatHistory == null)
            {
                throw new ChatHistoryNotFoundException($"Chat history with id {id} not found");
            }

            try
            {
                databaseContext.ChatHistories.Remove(chatHistory);
                databaseContext.SaveChanges();
            }
            catch (DbUpdateException e)
            {
                throw new UnexpectedDatabaseExcception($"Unexpected database execution error: {e.Message}");
            }
        }

        public void DeleteAllChatHistoriesForUser(int userId)
        {
            if (!userService.UserExists(userId))
            {
                throw new UserNotFoundException($"User with id {userId} not found");
            }

            var chatHistories = databaseContext.ChatHistories
                .Where(ch => ch.UserId == userId)
                .ToList();

            try
            {
                databaseContext.ChatHistories.RemoveRange(chatHistories);
                databaseContext.SaveChanges();
            }
            catch (DbUpdateException e)
            {
                throw new UnexpectedDatabaseExcception($"Unexpected database execution error: {e.Message}");
            }
        }

        // Utility
        public bool ChatHistoryExists(string id, int userId)
        {
            return databaseContext.ChatHistories
                .Any(ch => ch.Id == id && ch.UserId == userId);
        }

        public string GenerateChatHistoryId()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var random = Guid.NewGuid().ToString("N").Substring(0, 12);
            return $"conv_{timestamp}_{random}";
        }
    }
}
