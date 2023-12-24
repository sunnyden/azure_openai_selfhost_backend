namespace OpenAISelfhost.Exceptions.Http
{
    public class InvalidPayloadException : HttpException
    {
        public InvalidPayloadException(string message) : base(StatusCodes.Status400BadRequest, message) { }
    }
}
