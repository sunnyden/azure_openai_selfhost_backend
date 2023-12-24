using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAISelfhost.DataContracts.Request.User
{
    public record LoginRequest
    {
        [Required(ErrorMessage = "User name is required.")]
        public string UserName { get; init; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; init; } = null!;
    }
}
