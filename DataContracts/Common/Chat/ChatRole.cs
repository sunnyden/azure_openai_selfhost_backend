using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAISelfhost.DataContracts.Common.Chat
{
    public enum ChatRole
    {
        System,
        User,
        Assistant
    }

    public static class ChatRoleExtensions
    {
        public static string ToRoleString(this ChatRole role)
        {
            switch (role)
            {
                case ChatRole.System:
                    return "system";
                case ChatRole.User:
                    return "user";
                case ChatRole.Assistant:
                    return "assistant";
                default:
                    throw new ArgumentException("Invalid role");
            }
        }

        public static ChatRole ToChatRole(this string role)
        {
            switch (role)
            {
                case "system":
                    return ChatRole.System;
                case "user":
                    return ChatRole.User;
                case "assistant":
                    return ChatRole.Assistant;
                default:
                    throw new ArgumentException("Invalid role");
            }
        }
    }
}
