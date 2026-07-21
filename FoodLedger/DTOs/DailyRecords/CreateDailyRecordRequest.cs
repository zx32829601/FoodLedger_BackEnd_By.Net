namespace FoodLedger.DTOs.DailyRecords;

/// <summary>
/// 建立每日飲食紀錄的請求資料。
/// </summary>
/// <remarks>
/// 請求不可包含使用者 ID；紀錄擁有者必須由後端透過目前登入使用者決定。
/// </remarks>
public sealed class CreateDailyRecordRequest
{
    /// <summary>
    /// 食物資料識別碼。
    /// </summary>
    public long FoodId { get; init; }

    /// <summary>
    /// 食用數量。
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// 食用時間，應使用 UTC。
    /// </summary>
    public DateTimeOffset ConsumedAt { get; init; }
}
