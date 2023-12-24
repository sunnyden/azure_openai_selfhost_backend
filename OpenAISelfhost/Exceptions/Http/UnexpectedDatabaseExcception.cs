namespace OpenAISelfhost.Exceptions.Http
{
    public class UnexpectedDatabaseExcception : HttpException
    {
        public UnexpectedDatabaseExcception(string message) : base(StatusCodes.Status500InternalServerError, message) { }
    }
}
