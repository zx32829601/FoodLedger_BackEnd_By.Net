using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoodLedger.Models;

namespace FoodLedger.Data.Entities
{
    /// <summary>代表營養素在指定語系下的顯示名稱。</summary>
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
        [MaxLength(LocalizationRules.MaximumLangCodeLength)]
        [Column("lang_code")]
        public required string LangCode { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("nutrient_name")]
        public required string NutrientName { get; set; }
    }
}
