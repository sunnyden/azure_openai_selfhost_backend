using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace OpenAISelfhost.Middleware
{
    public class DisableCompressionForSSEMiddleware
    {
        private readonly RequestDelegate _next;

        public DisableCompressionForSSEMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Disable compression for streaming endpoints
            if (context.Request.Path.ToString().Contains("streamingCompletion"))
            {
                // Set header to disable compression specifically
                context.Response.Headers.Append("Content-Encoding", "identity");
                context.Response.Headers.Append("Transfer-Encoding", "identity");
            }

            await _next(context);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline
    public static class DisableCompressionForSSEMiddlewareExtensions
    {
        public static IApplicationBuilder UseDisableCompressionForSSE(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DisableCompressionForSSEMiddleware>();
        }
    }
}
