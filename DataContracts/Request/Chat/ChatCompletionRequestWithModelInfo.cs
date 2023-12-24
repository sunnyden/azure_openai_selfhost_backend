using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAISelfhost.DataContracts.Request.Chat
{
    public class ChatCompletionRequestWithModelInfo
    {
        public string Model { get; set; }
        public ChatCompletionRequest Request { get; set; }
    }
}
