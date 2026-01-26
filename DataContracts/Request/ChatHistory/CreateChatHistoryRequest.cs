using OpenAISelfhost.DataContracts.Common.Chat;

namespace OpenAISelfhost.DataContracts.Request.ChatHistory
{
    public class CreateChatHistoryRequest
    {
        public string? Title { get; set; }
        public List<ChatMessage> Messages { get; set; }
    }
}
