using OpenAISelfhost.DataContracts.Utils.Serialization.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenAISelfhost.DataContracts.Common.Chat
{
    public class ChatMessage
    {
        [JsonConverter(typeof(JsonChatRoleConverter))]
        public ChatRole Role { get; set; }
        public IEnumerable<ChatContentItem> Content { get; set; }
    }
}
