using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenAISelfhost.DataContracts.Request.ChatModel
{
    public class ChatModelModifyRequest
    {
        public string Identifier { get; set; }

        public string FriendlyName { get; set; }

        public string Endpoint { get; set; }

        public string Deployment { get; set; }

        public string? Key { get; set; }

        public double CostPromptToken { get; set; }

        public double CostResponseToken { get; set; }

        public bool IsVision { get; set; }

        public int MaxTokens { get; set; }

        public bool SupportTool { get; set; }

        public string? ApiVersionOverride { get; set; }
    }
}
