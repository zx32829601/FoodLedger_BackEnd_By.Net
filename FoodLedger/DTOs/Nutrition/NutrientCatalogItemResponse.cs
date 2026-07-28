namespace FoodLedger.DTOs.Nutrition;

/// <summary>
/// 建立食物表單與其他 client 使用的營養素目錄項目。
/// </summary>
public sealed class NutrientCatalogItemResponse
{
    /// <summary>營養素識別碼。</summary>
    public long NutrientId { get; init; }

    /// <summary>跨語系穩定的營養素代碼。</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>依指定語系 fallback 規則選出的顯示名稱。</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>實際採用的翻譯語系；沒有翻譯而使用 code 時為 null。</summary>
    public string? LangCode { get; init; }

    /// <summary>營養素計量單位代碼。</summary>
    public string UnitCode { get; init; } = string.Empty;
}
