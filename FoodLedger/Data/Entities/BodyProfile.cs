using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities;

/// <summary>
/// 儲存使用者用於熱量與營養目標計算的身體資料。
/// </summary>
[Table("body_profile")]
public sealed class BodyProfile : BaseEntity
{
    [Key]
    [Column("user_id")]
    public long UserId { get; set; }

    [Column("birth_date")]
    public DateOnly BirthDate { get; set; }

    [MaxLength(20)]
    [Column("biological_sex_code")]
    public required string BiologicalSexCode { get; set; }

    [Column("height_in_centimeters")]
    public decimal HeightInCentimeters { get; set; }

    [MaxLength(50)]
    [Column("fitness_goal_code")]
    public required string FitnessGoalCode { get; set; }

    [MaxLength(50)]
    [Column("activity_level_code")]
    public required string ActivityLevelCode { get; set; }

    [MaxLength(255)]
    [Column("time_zone")]
    public required string TimeZone { get; set; }

    [ConcurrencyCheck]
    [Column("version")]
    public Guid Version { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;
}
