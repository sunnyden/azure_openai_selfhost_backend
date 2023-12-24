namespace OpenAISelfhost.Exceptions.Http
{
    public class HttpException : Exception
    {
        public int StatusCode { get; set; }
        public HttpException(int code,string message) : base(message)
        {
            StatusCode = code;
        }
    }
}
