using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities
{
    /// <summary>代表可被搜尋、記錄並維護營養資料的食物主檔。</summary>
    [Table("simple_food")]
    public class SimpleFood : BaseEntity
    {
        [Key]
        [Column("food_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long FoodId { get; set; }

        [Column("food_code")]
        [MaxLength(50)]
        [Required]
        public required string FoodCode { get; set; }

        public virtual ICollection<SimpleFoodTranslation> Translations { get; set; } = [];

    }
}
