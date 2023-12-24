using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAISelfhost.DataContracts.DataTables;
using OpenAISelfhost.DataContracts.Enums;
using OpenAISelfhost.DataContracts.Request.User;
using OpenAISelfhost.DataContracts.Response.Common;
using OpenAISelfhost.Exceptions.Http;
using OpenAISelfhost.Service;
using OpenAISelfhost.Service.Interface;

namespace OpenAISelfhost.Controllers
{
    [Route("user")]
    [ApiController]
    public class UserController : ApiControllerBase
    {
        private readonly IUserService userService;

        public UserController(IUserService userService)
        {
            this.userService = userService;
        }

        [HttpPost("auth")]
        public ApiResponse<string> Auth([FromBody] LoginRequest request)
        {
            var token = userService.GetAuthorizationToken(request.UserName, request.Password);
            return new ApiResponse<string>()
            {
                Data = token
            };
        }

        [HttpGet("me")]
        [Authorize]
        public ApiResponse<User> Me()
        {
            return new()
            {
                Data = userService.GetUser(GetUserId())
            };
        }

        [HttpPost("create")]
        [Authorize(Roles = UserType.Admin)]
        public ApiResponse<User> CreateUser([FromBody] UserModifyRequest request)
        {
            userService.CreateUser(request.UserName, request.Password, request.IsAdmin, request.RemainingCredit, request.CreditQuota);
            return new() { Data = userService.GetUser(request.UserName) };
        }

        [HttpPost("update")]
        [Authorize(Roles = UserType.Admin)]
        public ApiResponse<User> UpdateUser([FromBody] UserModifyRequest request)
        {
            var newUser = userService.GetUser(request.Id.Value);
            if(newUser == null)
                throw new UserNotFoundException("User not found");
            newUser.IsAdmin = request.IsAdmin;
            newUser.RemainingCredit = request.RemainingCredit;
            newUser.CreditQuota = request.CreditQuota;
            newUser.UserName = request.UserName;
            if (!string.IsNullOrEmpty(request.Password))
            {
                newUser.Password = UserService.GetPasswordHash(request.UserName, request.Password);
            }
            
            userService.UpdateUser(newUser);
            return new() { Data = newUser };
        }

        [HttpGet("list")]
        [Authorize(Roles = UserType.Admin)]
        public ApiResponse<IEnumerable<User>> ListUsers()
        {
            return new()
            {
                Data = userService.GetUsers()
            };
        }

        [HttpPost("delete")]
        [Authorize(Roles = UserType.Admin)]
        public ApiResponse<User> DeleteUser([FromBody] DeleteUserRequest request)
        {
            var user = userService.GetUser(request.UserId);
            if(user == null)
                throw new UserNotFoundException($"User with id {request.UserId} not found");
            userService.DeleteUser(user);
            return new() { Data = user };
        }

        [HttpPost("update-password")]
        [Authorize]
        public ApiResponse<string> UpdatePassword([FromBody] ChangePasswordRequest request)
        {
            var user = userService.GetUser(GetUserId());
            if(user == null)
                throw new UserNotFoundException($"User with id {GetUserId()} not found");
            if(user.Password != UserService.GetPasswordHash(user.UserName, request.OldPassword))
                throw new UnauthorizedAccessException("Old password is incorrect");
            user.Password = UserService.GetPasswordHash(user.UserName, request.NewPassword);
            userService.UpdateUser(user);
            return new();
        }

    }
}
