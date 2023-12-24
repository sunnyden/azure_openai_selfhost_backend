using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAISelfhost.DataContracts.Response.Chat
{
    public class ChatResponse
    {
        public string Id { get; set; }
        public string StopReason { get; set; }
        public string Message { get; set; }
        public int PromptTokens { get; set; }
        public int ResponseTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
