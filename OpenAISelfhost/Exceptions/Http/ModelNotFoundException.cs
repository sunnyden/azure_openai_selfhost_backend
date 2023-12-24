namespace OpenAISelfhost.Exceptions.Http
{
    public class ModelNotFoundException : HttpException
    {
        public ModelNotFoundException(string message) : base(StatusCodes.Status404NotFound, message) { }
    }
}
