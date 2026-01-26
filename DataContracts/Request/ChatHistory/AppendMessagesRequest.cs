using OpenAISelfhost.DataContracts.Common.Chat;

namespace OpenAISelfhost.DataContracts.Request.ChatHistory
{
    public class AppendMessagesRequest
    {
        public string Id { get; set; }
        public List<ChatMessage> Messages { get; set; }
    }
}
