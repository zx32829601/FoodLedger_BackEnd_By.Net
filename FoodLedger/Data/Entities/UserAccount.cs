using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace FoodLedger.Data.Entities
{
    [Table("user_account")]
    public class UserAccount : BaseEntity
    {
        [Key]
        [Column("user_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserId { get; set; }

        [Column("account")]
        [MaxLength(50)]
        public string Account { get; set; } = string.Empty;

        [Column("password_hash")]
        [MaxLength(200)]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("display_name")]
        [MaxLength(50)]
        public string DisplayName { get; set; } = string.Empty;

        [Column("email")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Column("account_status")]
        [MaxLength(100)]
        public byte AccountStatus { get; set; }
    }
}
