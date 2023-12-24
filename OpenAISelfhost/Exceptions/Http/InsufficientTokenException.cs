namespace OpenAISelfhost.Exceptions.Http
{
    public class InsufficientTokenException : HttpException
    {
        public InsufficientTokenException(string message) : base(StatusCodes.Status402PaymentRequired, message) { }
    }
}
