using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAISelfhost.DataContracts.Common.Chat
{
    public enum ConversationRole
    {
        System,
        User,
        Assistant
    }

    public static class ChatRoleExtensions
    {
        public static string ToRoleString(this ConversationRole role)
        {
            switch (role)
            {
                case ConversationRole.System:
                    return "system";
                case ConversationRole.User:
                    return "user";
                case ConversationRole.Assistant:
                    return "assistant";
                default:
                    throw new ArgumentException("Invalid role");
            }
        }

        public static ConversationRole ToChatRole(this string role)
        {
            switch (role)
            {
                case "system":
                    return ConversationRole.System;
                case "user":
                    return ConversationRole.User;
                case "assistant":
                    return ConversationRole.Assistant;
                default:
                    throw new ArgumentException("Invalid role");
            }
        }
    }
}
