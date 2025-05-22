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
            if(model == null)
                throw new ModelNotFoundException("Model not found");
            var response = await chatService.RequestCompletion(model, request.Request, GetUserId());
            return new()
            {
                Data = response
            };
        }
    }
}
