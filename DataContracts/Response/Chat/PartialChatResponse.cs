using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAISelfhost.DataContracts.Response.Chat
{
    public class PartialChatResponse
    {
        public string Data { get; set; }
        public string FinishReason { get; set; }
        public bool IsEnd { get; set; }
    }
}
