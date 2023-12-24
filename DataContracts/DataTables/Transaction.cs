using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAISelfhost.DataContracts.DataTables
{
    [Table("transaction")]
    public class Transaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }
        [Column("time")]
        public DateTime Time { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("transaction_id")]
        public string TransactionId { get; set; }
        [Column("requested_service")]
        public string RequestedService { get; set; }
        [Column("prompt_token")]
        public int PromptTokens { get; set; }
        [Column("response_token")]
        public int ResponseTokens { get; set; }
        [Column("total_token")]
        public int TotalTokens { get; set; }
        [Column("cost")]
        public double Cost { get; set; }
    }
}
