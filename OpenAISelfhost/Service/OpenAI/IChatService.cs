using OpenAISelfhost.DataContracts.DataTables;
using OpenAISelfhost.DataContracts.Request.Chat;
using OpenAISelfhost.DataContracts.Response.Chat;

namespace OpenAISelfhost.Service.OpenAI
{
    public interface IChatService
    {
        public Task<ChatResponse> RequestCompletion(ChatModel model, ChatCompletionRequest request, int userId);
        public IAsyncEnumerable<PartialChatResponse> RequestStreamingCompletion(ChatModel model, ChatCompletionRequest request, int userId);
    }
}
