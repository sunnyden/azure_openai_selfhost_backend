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
        Image
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
                    return "image_url";
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
                case "image_url":
                    return ChatContentType.Image;
                default:
                    throw new ArgumentException("Invalid content type");
            }
        }
    }
}
