using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OpenAISelfhost.DataContracts.DataTables
{
    [Table("chat_model")]
    public class ChatModel
    {
        [Key]
        [Column("identifier")]
        public string Identifier { get; set; }

        [Column("friendly_name")]
        public string FriendlyName { get; set; }

        [Column("endpoint")]
        public string Endpoint { get; set; }

        [Column("deployment")]
        public string Deployment { get; set; }

        [JsonIgnore]
        [Column("access_key")]
        public string Key { get; set; }

        [Column("cost_prompt_token")]
        public double CostPromptToken { get; set; }

        [Column("cost_response_token")]
        public double CostResponseToken { get; set; }

        [Column("is_vision")]
        public bool IsVision { get; set; }

        [Column("max_tokens")]
        public int MaxTokens { get; set; }

        [Column("use_tool")]
        public bool SupportTool { get; set; }

        [Column("api_version_override")]
        public string? ApiVersionOverride { get; set; }
    }
}
