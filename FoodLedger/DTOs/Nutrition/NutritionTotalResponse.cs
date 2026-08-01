namespace FoodLedger.DTOs.Nutrition;

/// <summary>
/// 指定期間彙總後的一筆動態營養素資料。
/// </summary>
public sealed class NutritionTotalResponse
{
    /// <summary>營養素識別碼。</summary>
    public long NutrientId { get; init; }

    /// <summary>營養素穩定代碼。</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>預設語系顯示名稱。</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>實際採用的翻譯語系；沒有翻譯而使用 code 時為 null。</summary>
    public string? LangCode { get; init; }

    /// <summary>彙總後數值。</summary>
    public decimal Amount { get; init; }

    /// <summary>營養素單位代碼。</summary>
    public string UnitCode { get; init; } = string.Empty;

    /// <summary>跨 client 一致的全域顯示順位。</summary>
    public int DisplayOrder { get; init; }
}
