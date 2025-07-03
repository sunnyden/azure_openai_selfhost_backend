using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAISelfhost.DataContracts.DataTables;
using OpenAISelfhost.DataContracts.Enums;
using OpenAISelfhost.DataContracts.Request.ChatModel;
using OpenAISelfhost.DataContracts.Response.Common;
using OpenAISelfhost.Exceptions.Http;
using OpenAISelfhost.Service.OpenAI;

namespace OpenAISelfhost.Controllers
{
    [Route("model")]
    [ApiController]
    public class ModelController : ApiControllerBase
    {
        private readonly IModelService modelService;

        public ModelController(IModelService modelService)
        {
            this.modelService = modelService;
        }

        [HttpGet("list")]
        [Authorize]
        public ApiResponse<IEnumerable<ChatModel>> ListModels()
        {
            return new()
            {
                Data = modelService.GetModelsForUser(GetUserId())
            };
        }

        [HttpGet("all")]
        [Authorize(Roles = UserType.Admin)]
        public ApiResponse<IEnumerable<ChatModel>> ListAllModels()
        {
            return new()
            {
                Data = modelService.GetModels()
            };
        }

        [HttpPost("add")]
        [Authorize(Roles = UserType.Admin)]
        public ApiResponse<string> AddModel([FromBody] ChatModelModifyRequest request)
        {
            if (string.IsNullOrEmpty(request.Key))
                throw new InvalidPayloadException("Access key must be provided");
            var model = new ChatModel()
            {
                Identifier = request.Identifier,
                FriendlyName = request.FriendlyName,
                Key = request.Key,
                IsVision = request.IsVision,
                Endpoint = request.Endpoint,
                Deployment = request.Deployment,
                MaxTokens = request.MaxTokens,
                CostResponseToken = request.CostResponseToken,
                CostPromptToken = request.CostPromptToken,
                SupportTool = request.SupportTool
            };
            modelService.AddModel(model);
            return new();
        }

        [HttpPost("update")]
        [Authorize(Roles = UserType.Admin)]
        public ApiResponse<string> UpdateModel([FromBody] ChatModelModifyRequest request)
        {
            var newModel = modelService.GetModel(request.Identifier);
            if(newModel == null)
                throw new ModelNotFoundException("Model not found");
            if(!string.IsNullOrEmpty(request.Key))
                newModel.Key = request.Key;
            newModel.FriendlyName = request.FriendlyName;

            newModel.IsVision = request.IsVision;
            newModel.Endpoint = request.Endpoint;
            newModel.Deployment = request.Deployment;
            newModel.MaxTokens = request.MaxTokens;

            newModel.CostResponseToken = request.CostResponseToken;
            newModel.CostPromptToken = request.CostPromptToken;
            newModel.SupportTool = request.SupportTool;
            modelService.UpdateModel(newModel);
            return new();
        }

        [HttpPost("delete")]
        [Authorize(Roles = UserType.Admin)]
        public ApiResponse<string> DeleteModel([FromBody] DeleteModelRequest model)
        {
            modelService.DeleteModel(model.Model);
            return new();
        }

        [HttpPost("assign")]
        [Authorize(Roles = UserType.Admin)]
        public ApiResponse<string> AssignModel([FromBody] UserModelAssignment assignment)
        {
            modelService.AssignModelToUser(assignment.UserId, assignment.ModelIdentifier);
            return new();
        }

        [HttpPost("unassign")]
        [Authorize(Roles = UserType.Admin)]
        public ApiResponse<string> UnassignModel([FromBody] UserModelAssignment assignment)
        {
            modelService.UnassignModelFromUser(assignment.UserId, assignment.ModelIdentifier);
            return new();
        }
    }
}
