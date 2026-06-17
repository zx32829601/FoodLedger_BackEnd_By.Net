using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities
{
    [Table("food_category_translation")]
    public class FoodCategoryTranslation : BaseEntity
    {
        [Key]
        [Column("translation_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long TranslationId { get; set; }

        [Required]
        [Column("category_id")]
        public long CategoryId { get; set; }

        [Required]
        [MaxLength(10)]
        [Column("lang_code")] // 例如: "zh-TW", "en-US"
        public required string LangCode { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("category_name")]
        public required string CategoryName { get; set; }

        [ForeignKey("CategoryId")]
        public virtual FoodCategory FoodCategory { get; set; } = null!;
    }
}
