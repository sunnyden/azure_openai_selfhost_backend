using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAISelfhost.DataContracts.DataTables
{
    [Table("user_model_assignment")]
    public class UserModelAssignment
    {
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("model_identifier")]
        public string ModelIdentifier { get; set; }
    }
}
