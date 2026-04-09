using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities
{
    [Table("simple_food_translation")]
    public class SimpleFoodTranslation : BaseEntity
    {
        [Key]
        [Column("translation_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long TranslationId { get; set; }

        [Column("food_id")]
        public long FoodId { get; set; }

        // 設定外鍵關聯到 SimpleFood
        [ForeignKey(nameof(FoodId))]
        public virtual SimpleFood Food { get; set; } = default!;

        [Column("lang_code")]
        [MaxLength(10)]
        [Required]
        public required string LangCode { get; set; }

        [Column("food_name")]
        [MaxLength(200)]
        [Required]
        public required string FoodName { get; set; }

        [Column("description")]
        public string Description { get; set; } = string.Empty;
    }
}
