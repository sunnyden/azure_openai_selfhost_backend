using Microsoft.AspNetCore.Http;

namespace OpenAISelfhost.Exceptions.Http
{
    public class ChatHistoryNotFoundException : HttpException
    {
        public ChatHistoryNotFoundException(string message)
            : base(StatusCodes.Status404NotFound, message) { }
    }
}
