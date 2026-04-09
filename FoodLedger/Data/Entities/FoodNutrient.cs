using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities
{
    [Table("food_nutrient")]
    public class FoodNutrient : BaseEntity
    {
        [Column("food_id")]
        [Required]
        public long FoodId { get; set; }

        [Column("nutrient_id")]
        [Required]
        public long NutrientId { get; set; }

        [Column("amount")]
        [Required]
        public decimal Amount { get; set; }

        [Column("per_unit")]
        public string PerUnit { get; set; } = "'100g";

        // 導覽屬性 (Navigation Properties)
        [ForeignKey(nameof(FoodId))]
        public virtual SimpleFood Food { get; set; } = null!;

        [ForeignKey(nameof(NutrientId))]
        public virtual Nutrient Nutrient { get; set; } = null!;
    }
}
