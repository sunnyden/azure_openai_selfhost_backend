using OpenAISelfhost.DataContracts.Common.Chat;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenAISelfhost.DataContracts.Utils.Serialization.Chat
{
    public class JsonChatContentTypeConverter : JsonConverter<ChatContentType>
    {
        public override ChatContentType Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString().ToChatContentType();
        }

        public override void Write(Utf8JsonWriter writer, ChatContentType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToContentTypeString());
        }
    }
}
