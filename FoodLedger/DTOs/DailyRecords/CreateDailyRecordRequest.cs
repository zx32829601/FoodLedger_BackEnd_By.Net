using System.ComponentModel.DataAnnotations;

namespace FoodLedger.DTOs.DailyRecords;

/// <summary>
/// 建立每日飲食紀錄的請求資料。
/// </summary>
/// <remarks>
/// 請求不可包含使用者 ID；紀錄擁有者必須由後端透過目前登入使用者決定。
/// </remarks>
public sealed class CreateDailyRecordRequest
{
    private const string MinimumQuantity = "0.001";

    private const string MaximumQuantity = "10000";

    /// <summary>
    /// 食物資料識別碼。
    /// </summary>
    public long FoodId { get; init; }

    /// <summary>
    /// 食用數量。
    /// </summary>
    /// <remarks>
    /// 數量必須介於 0.001 到 10000 之間，避免建立沒有實際攝取量或明顯不合理的飲食紀錄。
    /// </remarks>
    [Range(typeof(decimal), MinimumQuantity, MaximumQuantity)]
    public decimal Quantity { get; init; }

    /// <summary>
    /// 食用時間，應使用 UTC。
    /// </summary>
    public DateTimeOffset ConsumedAt { get; init; }
}
