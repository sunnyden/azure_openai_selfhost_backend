namespace OpenAISelfhost.Exceptions.Http
{
    public class AuthorizationException : HttpException
    {
        public AuthorizationException(string message) : base(StatusCodes.Status401Unauthorized, message)
        {
        }
    }
}
