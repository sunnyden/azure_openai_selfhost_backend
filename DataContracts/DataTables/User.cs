using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OpenAISelfhost.DataContracts.DataTables
{
    [Table("user")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }
        [Column("user_name")]
        public string UserName { get; set; }
        [JsonIgnore]
        [Column("password")]
        public string Password { get; set; }
        [Column("is_admin")]
        public bool IsAdmin { get; set; }
        [Column("remaining_credit")]
        public double RemainingCredit { get; set; }
        [Column("credit_quota")]
        public double CreditQuota { get; set; }
    }
}
