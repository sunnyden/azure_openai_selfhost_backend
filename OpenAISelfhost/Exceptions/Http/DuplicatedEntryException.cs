namespace OpenAISelfhost.Exceptions.Http
{
    public class DuplicatedEntryException : HttpException
    {
        public DuplicatedEntryException(string message) : base(StatusCodes.Status409Conflict, message) { }
    }
}
