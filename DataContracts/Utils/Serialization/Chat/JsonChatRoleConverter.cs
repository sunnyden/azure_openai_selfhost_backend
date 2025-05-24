using OpenAISelfhost.DataContracts.Common.Chat;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenAISelfhost.DataContracts.Utils.Serialization.Chat
{
    public class JsonChatRoleConverter : JsonConverter<ConversationRole>
    {
        public override ConversationRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString().ToChatRole();
        }

        public override void Write(Utf8JsonWriter writer, ConversationRole value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToRoleString());
        }
    }
}
