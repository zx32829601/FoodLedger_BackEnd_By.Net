using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities;

/// <summary>代表使用者在特定時間攝取某項食物的每日飲食紀錄。</summary>
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

    /// <summary>取得或設定實際攝取重量，單位為克。</summary>
    [Column("quantity")]
    public decimal Quantity { get; set; }

    /// <summary>取得或設定實際攝取時間；此值與餐別彼此獨立。</summary>
    [Column("consumed_at")]
    public DateTimeOffset ConsumedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>取得或設定餐別代碼；變更餐別不會改變攝取時間。</summary>
    [MaxLength(50)]
    [Column("meal_type_code")]
    public string MealTypeCode { get; set; } = MealTypeCodes.Snack;

    [MaxLength(500)]
    [Column("note")]
    public string? Note { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;

    [ForeignKey(nameof(FoodId))]
    public virtual SimpleFood Food { get; set; } = null!;
}
