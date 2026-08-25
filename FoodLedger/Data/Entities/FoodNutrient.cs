using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities
{
    /// <summary>代表食物在指定基準份量下所含的單一營養素數值。</summary>
    [Table("food_nutrient")]
    public class FoodNutrient : BaseEntity
    {
        [Column("food_id")]
        [Required]
        public long FoodId { get; set; }

        [Column("nutrient_id")]
        [Required]
        public long NutrientId { get; set; }

        /// <summary>取得或設定在 <see cref="PerUnit" /> 基準下的營養素含量。</summary>
        [Column("amount")]
        [Required]
        public decimal Amount { get; set; }

        /// <summary>取得或設定營養資料的克數基準，目前預設為每 100 克。</summary>
        [Column("per_unit")]
        [MaxLength(20)]
        public string PerUnit { get; set; } = "100";

        // 導覽屬性 (Navigation Properties)
        [ForeignKey(nameof(FoodId))]
        public virtual SimpleFood Food { get; set; } = null!;

        [ForeignKey(nameof(NutrientId))]
        public virtual Nutrient Nutrient { get; set; } = null!;
    }
}
