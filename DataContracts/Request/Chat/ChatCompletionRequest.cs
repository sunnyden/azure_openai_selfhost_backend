using OpenAISelfhost.DataContracts.Common.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenAISelfhost.DataContracts.Request.Chat
{
    public class ChatCompletionRequest
    {
        public IEnumerable<ChatMessage> Messages { get; set; }
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
        public bool Stream { get; set; }
    }
}
