using OpenAISelfhost.DataContracts.Common.Chat;
using OpenAISelfhost.DataContracts.DataTables;

namespace OpenAISelfhost.Service.ChatHistory
{
    public interface IChatHistoryService
    {
        // Create
        DataContracts.DataTables.ChatHistory CreateChatHistory(int userId, string? title, List<ChatMessage> messages);

        // Read
        DataContracts.DataTables.ChatHistory? GetChatHistory(string id, int userId);
        IEnumerable<DataContracts.DataTables.ChatHistory> GetChatHistoriesForUser(int userId);
        IEnumerable<DataContracts.DataTables.ChatHistory> GetAllChatHistories();  // Admin only

        // Update
        void UpdateChatHistoryTitle(string id, int userId, string newTitle);
        void UpdateChatHistory(string id, int userId, string? title, List<ChatMessage> messages);
        void AppendMessages(string id, int userId, List<ChatMessage> newMessages);

        // Delete
        void DeleteChatHistory(string id, int userId);
        void DeleteAllChatHistoriesForUser(int userId);  // Admin only

        // Utility
        bool ChatHistoryExists(string id, int userId);
        string GenerateChatHistoryId();
    }
}
