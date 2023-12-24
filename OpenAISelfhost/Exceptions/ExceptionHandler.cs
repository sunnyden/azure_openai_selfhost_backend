using Microsoft.AspNetCore.Diagnostics;
using OpenAISelfhost.DataContracts.Response.Common;
using OpenAISelfhost.Exceptions.Http;
using System.Text.Json;

namespace OpenAISelfhost.Exceptions
{
    public class ExceptionHandler : IExceptionHandler
    {
        public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // check exception type
            if (exception is HttpException httpException)
            {
                // set response status code
                httpContext.Response.StatusCode = httpException.StatusCode;
                httpContext.Response.ContentType = "application/json";
                // write exception message to response
                var response = new ApiResponse<string>()
                {
                    IsSuccess = false,
                    Error = httpException.Message
                };
                var json = JsonSerializer.Serialize(response);
                httpContext.Response.WriteAsync(json).ConfigureAwait(false);
                return new ValueTask<bool>(true);
            }
            return new ValueTask<bool>(false);
        }

        
    }
}
