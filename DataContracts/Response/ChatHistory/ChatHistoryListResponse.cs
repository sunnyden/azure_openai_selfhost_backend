namespace OpenAISelfhost.DataContracts.Response.ChatHistory
{
    public class ChatHistoryListResponse
    {
        public string Id { get; set; }
        public string? Title { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }
        public int MessageCount { get; set; }
    }
}
