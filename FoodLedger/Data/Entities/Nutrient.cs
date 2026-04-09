using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities
{
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

        public virtual ICollection<NutrientTranslation> Translations { get; set; } = [];

    }
}
