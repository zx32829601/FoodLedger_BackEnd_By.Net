using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities;

/// <summary>代表使用者在伺服器時間記錄的一筆身體測量。</summary>
[Table("body_measurement")]
public sealed class BodyMeasurement : BaseEntity
{
    [Key]
    [Column("measurement_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long MeasurementId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("weight_in_kilograms")]
    public decimal WeightInKilograms { get; set; }

    [Column("body_fat_percentage")]
    public decimal? BodyFatPercentage { get; set; }

    [Column("muscle_mass_in_kilograms")]
    public decimal? MuscleMassInKilograms { get; set; }

    /// <summary>取得或設定後端產生且不可由 client 修改的 UTC 測量時間。</summary>
    [Column("measured_at")]
    public DateTimeOffset MeasuredAt { get; set; }

    /// <summary>取得或設定樂觀並行版本。</summary>
    [ConcurrencyCheck]
    [Column("version")]
    public Guid Version { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;
}
