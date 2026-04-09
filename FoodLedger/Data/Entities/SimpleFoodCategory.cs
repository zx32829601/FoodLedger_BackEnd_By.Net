using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities
{
    [Table("simple_food_category")]
    public class SimpleFoodCategory : BaseEntity
    {
        [Column("food_id")]
        [Required]
        public long FoodId { get; set; }

        [Column("category_id")]
        [Required]
        public long CategoryId { get; set; }

        [ForeignKey(nameof(FoodId))]
        public virtual SimpleFood Food { get; set; } = null!;

        [ForeignKey(nameof(CategoryId))]
        public virtual FoodCategory Category { get; set; } = null!;
    }
}
