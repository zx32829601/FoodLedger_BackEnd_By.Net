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
    /// 前端顯示紀錄所需的食物摘要，避免逐筆查詢食物。
    /// </summary>
    public DailyRecordFoodResponse Food { get; init; } = new();

    /// <summary>依本筆克數換算後的動態營養素資料。</summary>
    public IReadOnlyList<DailyRecordNutrientResponse> Nutrients { get; init; } = [];

    /// <summary>
    /// 食用份量，單位為克。
    /// </summary>
    public decimal QuantityInGrams { get; init; }

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

/// <summary>
/// Daily Record 依實際克數換算後的一筆營養素。
/// </summary>
public sealed class DailyRecordNutrientResponse
{
    /// <summary>營養素識別碼。</summary>
    public long NutrientId { get; init; }
    /// <summary>營養素穩定代碼。</summary>
    public string Code { get; init; } = string.Empty;
    /// <summary>依指定語系 fallback 規則選出的顯示名稱。</summary>
    public string DisplayName { get; init; } = string.Empty;
    /// <summary>實際採用的翻譯語系；沒有翻譯而使用 code 時為 null。</summary>
    public string? LangCode { get; init; }
    /// <summary>換算後數值。</summary>
    public decimal Amount { get; init; }
    /// <summary>營養素單位代碼。</summary>
    public string UnitCode { get; init; } = string.Empty;
    /// <summary>跨畫面一致的顯示順序。</summary>
    public int DisplayOrder { get; init; }
}

/// <summary>
/// Daily Record response 內嵌的食物顯示摘要。
/// </summary>
public sealed class DailyRecordFoodResponse
{
    /// <summary>食物識別碼。</summary>
    public long FoodId { get; init; }
    /// <summary>食物穩定代碼。</summary>
    public string FoodCode { get; init; } = string.Empty;
    /// <summary>依 fallback 規則選出的顯示名稱。</summary>
    public string DisplayName { get; init; } = string.Empty;
    /// <summary>實際採用的翻譯語系。</summary>
    public string LangCode { get; init; } = string.Empty;
}
