using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenAISelfhost.DataContracts.Request.User
{
    public class UserModifyRequest
    {
        public int? Id { get; set; }
        public string UserName { get; set; }
        public string? Password { get; set; }
        public bool IsAdmin { get; set; }
        public double RemainingCredit { get; set; }
        public double CreditQuota { get; set; }
    }
}
