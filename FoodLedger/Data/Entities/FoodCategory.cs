using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities
{
    /// <summary>代表可供多項食物共用的分類主資料。</summary>
    [Table("food_category")]
    public class FoodCategory : BaseEntity
    {
        [Column("category_id")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CategoryId { get; set; }

        [Column("category_code")]
        [MaxLength(100)]
        [Required]
        public required string CategoryCode { get; set; }

        public virtual ICollection<FoodCategoryTranslation> Translations { get; set; } = [];
    }
}
