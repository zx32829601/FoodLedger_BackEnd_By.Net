using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities
{
    /// <summary>代表可套用至食物營養資料的營養素主檔。</summary>
    [Table("nutrient")]
    public class Nutrient : BaseEntity
    {
        [Column("nutrient_id")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long NutrientId { get; set; }

        [Column("nutrient_code")]
        [MaxLength(50)]
        [Required]
        public required string NutrientCode { get; set; }

        /// <summary>取得或設定標準化營養單位代碼。</summary>
        [Column("unit_code")]
        [MaxLength(NutrientRules.MaximumUnitCodeLength)]
        [Required]
        public string UnitCode { get; set; } = NutrientUnitCodes.Gram;

        /// <summary>取得或設定營養素在 UI 與 API 回應中的顯示順序。</summary>
        [Column("display_order")]
        public int DisplayOrder { get; set; } = 1000;

        public virtual ICollection<NutrientTranslation> Translations { get; set; } = [];

    }
}
