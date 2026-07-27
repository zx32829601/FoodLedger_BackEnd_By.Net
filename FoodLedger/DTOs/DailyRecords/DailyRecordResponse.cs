namespace FoodLedger.DTOs.DailyRecords;

/// <summary>
/// 回傳每日飲食紀錄查詢結果所需的公開資料。
/// </summary>
/// <remarks>
/// Response 僅包含前端顯示與後續操作所需欄位，不直接暴露資料庫 Entity。
/// </remarks>
public sealed class DailyRecordResponse
{
    /// <summary>
    /// 每日飲食紀錄識別碼。
    /// </summary>
    public long RecordId { get; init; }

    /// <summary>
    /// 食物識別碼。
    /// </summary>
    public long FoodId { get; init; }

    /// <summary>
    /// 食用份量。
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// 實際食用時間，Service 以 UTC 日期區間篩選。
    /// </summary>
    public DateTimeOffset ConsumedAt { get; init; }

    /// <summary>
    /// 餐別穩定代碼。
    /// </summary>
    public string MealTypeCode { get; init; } = string.Empty;

    /// <summary>
    /// 使用者補充的選填備註。
    /// </summary>
    public string? Note { get; init; }
}
