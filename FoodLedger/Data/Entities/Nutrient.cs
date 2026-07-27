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

        [Column("unit_code")]
        [MaxLength(NutrientRules.MaximumUnitCodeLength)]
        [Required]
        public string UnitCode { get; set; } = NutrientUnitCodes.Gram;

        public virtual ICollection<NutrientTranslation> Translations { get; set; } = [];

    }
}
