using Microsoft.AspNetCore.Mvc.Filters;

namespace OpenAISelfhost.Filters
{
    /// <summary>
    /// Filter attribute to disable response compression for SSE endpoints
    /// </summary>
    public class DisableCompressionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Set headers to explicitly disable compression
            context.HttpContext.Response.Headers.Append("Content-Encoding", "identity");
            context.HttpContext.Response.Headers.Append("Transfer-Encoding", "identity");
            
            base.OnActionExecuting(context);
        }
    }
}
