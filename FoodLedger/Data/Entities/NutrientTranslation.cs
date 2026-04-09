using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities
{
    [Table("nutrient_translation")]
    public class NutrientTranslation : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("translation_id")]
        public long TranslationId { get; set; }

        [Required]
        [Column("nutrient_id")]
        public long NutrientId { get; set; }
        [ForeignKey(nameof(NutrientId))]

        public virtual Nutrient Nutrient { get; set; } = default!;

        [Required]
        [MaxLength(10)]
        [Column("lang_code")]
        public required string LangCode { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("nutrient_name")]
        public required string NutrientName { get; set; }
    }
}