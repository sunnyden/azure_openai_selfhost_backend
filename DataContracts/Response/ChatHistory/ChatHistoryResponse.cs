using OpenAISelfhost.DataContracts.Common.Chat;

namespace OpenAISelfhost.DataContracts.Response.ChatHistory
{
    public class ChatHistoryResponse
    {
        public string Id { get; set; }
        public string? Title { get; set; }
        public List<ChatMessage> Messages { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }
    }
}
