using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Json;

namespace OpenAISelfhost.Controllers
{
    public class ApiControllerBase : ControllerBase
    {
        public int GetUserId()
        {
            return int.Parse(User.Claims.Where(c => c.Type == ClaimTypes.Upn).Select(c => c.Value).FirstOrDefault("-1"));
        }
    }
}
