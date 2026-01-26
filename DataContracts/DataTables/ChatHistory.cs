using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAISelfhost.DataContracts.DataTables
{
    [Table("chat_history")]
    public class ChatHistory
    {
        [Column("id")]
        public string Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("title")]
        public string? Title { get; set; }

        [Column("messages")]
        public string? Messages { get; set; }  // JSON stored as longtext

        [Column("created_at")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public string? UpdatedAt { get; set; }  // varchar(45) - ISO 8601 string
    }
}
