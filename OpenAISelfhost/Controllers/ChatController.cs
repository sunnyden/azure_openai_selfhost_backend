using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAISelfhost.DataContracts.Request.Chat;
using OpenAISelfhost.DataContracts.Response.Chat;
using OpenAISelfhost.DataContracts.Response.Common;
using OpenAISelfhost.Exceptions.Http;
using OpenAISelfhost.Service.OpenAI;

namespace OpenAISelfhost.Controllers
{
    [Route("chat")]
    [Authorize]
    [ApiController]
    public class ChatController : ApiControllerBase
    {
        private readonly IChatService chatService;
        private readonly IModelService modelService;

        public ChatController(IChatService chatService, IModelService modelService)
        {
            this.chatService = chatService;
            this.modelService = modelService;
        }

        [HttpPost("completion")]
        public async Task<ApiResponse<ChatResponse>> RequestCompletion(
            [FromBody] ChatCompletionRequestWithModelInfo request)
        {
            var model = modelService.GetModelForUser(GetUserId(), request.Model);
            if (model == null)
                throw new ModelNotFoundException("Model not found");
            var response = await chatService.RequestCompletion(model, request.Request, GetUserId());
            return new()
            {
                Data = response
            };
        }

        /**
         * @param request
         * @return service sent event event stream
         */
        [HttpPost("streamingCompletion")]
        public async Task StreamingCompletion(
            [FromBody] ChatCompletionRequestWithModelInfo request)
        {
            var model = modelService.GetModelForUser(GetUserId(), request.Model);
            if (model == null)
                throw new ModelNotFoundException("Model not found");
            var response = chatService.RequestStreamingCompletion(model, request.Request, GetUserId());
            
            // Set headers for SSE and explicitly disable caching and compression
            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Cache-Control", "no-cache, no-store, max-age=0, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("X-Content-Type-Options", "nosniff");
            // Explicitly tell proxies not to buffer or compress the response
            Response.Headers.Append("X-Accel-Buffering", "no");
            // Explicitly set content encoding to identity to disable compression
            Response.Headers.Append("Content-Encoding", "identity");
            Response.Headers.Append("Transfer-Encoding", "identity");
            
            // Send the initial event
            await foreach (var chunk in response)
            {
                // Send each chunk as a separate event
                // serialize as one line json with explicit options to ensure no indentation
                var jsonOptions = new System.Text.Json.JsonSerializerOptions { WriteIndented = false };
                jsonOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                var json = System.Text.Json.JsonSerializer.Serialize(chunk, jsonOptions);
                await Response.WriteAsync($"data: {json}\n\n");
                await Response.Body.FlushAsync();
            }
            // Send the final event
            await Response.Body.FlushAsync();
            // Complete the response
            await Response.CompleteAsync();
        }
    }
}
