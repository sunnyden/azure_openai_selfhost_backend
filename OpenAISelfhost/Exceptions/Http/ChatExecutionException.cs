namespace OpenAISelfhost.Exceptions.Http
{
    public class ChatExecutionException : HttpException
    {
        public ChatExecutionException(string message) : base(StatusCodes.Status500InternalServerError, message) { }
    }
}
