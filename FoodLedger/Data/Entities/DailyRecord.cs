using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities;

[Table("daily_record")]
public class DailyRecord : BaseEntity
{
    [Key]
    [Column("record_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long RecordId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("food_id")]
    public long FoodId { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("consumed_at")]
    public DateTimeOffset ConsumedAt { get; set; } = DateTimeOffset.UtcNow;
}