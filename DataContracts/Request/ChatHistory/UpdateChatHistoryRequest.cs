using OpenAISelfhost.DataContracts.Common.Chat;

namespace OpenAISelfhost.DataContracts.Request.ChatHistory
{
    public class UpdateChatHistoryRequest
    {
        public string Id { get; set; }
        public string? Title { get; set; }
        public List<ChatMessage> Messages { get; set; }
    }
}
