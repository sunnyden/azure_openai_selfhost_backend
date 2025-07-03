using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAISelfhost.DataContracts.Common.Chat
{
    public enum ChatContentType
    {
        Text,
        Image,
        Audio
    }

    public static class ChatContentTypeExtensions
    {
        public static string ToContentTypeString(this ChatContentType type)
        {
            switch (type)
            {
                case ChatContentType.Text:
                    return "text";
                case ChatContentType.Image:
                    return "image";
                case ChatContentType.Audio:
                    return "audio";
                default:
                    throw new ArgumentException("Invalid content type");
            }
        }

        public static ChatContentType ToChatContentType(this string type)
        {
            switch (type)
            {
                case "text":
                    return ChatContentType.Text;
                case "image":
                    return ChatContentType.Image;
                case "audio":
                    return ChatContentType.Audio;
                default:
                    throw new ArgumentException("Invalid content type");
            }
        }
    }
}
