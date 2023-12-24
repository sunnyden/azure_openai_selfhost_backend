namespace OpenAISelfhost.Exceptions.Http
{
    public class UserNotFoundException : HttpException
    {
        public UserNotFoundException(string message) : base(StatusCodes.Status404NotFound, message) { }
    }
}
