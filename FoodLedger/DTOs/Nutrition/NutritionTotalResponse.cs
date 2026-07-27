namespace FoodLedger.DTOs.Nutrition;

/// <summary>
/// 指定期間彙總後的一筆動態營養素資料。
/// </summary>
public sealed class NutritionTotalResponse
{
    /// <summary>營養素穩定代碼。</summary>
    public string Code { get; init; } = string.Empty;
    /// <summary>預設語系顯示名稱。</summary>
    public string DisplayName { get; init; } = string.Empty;
    /// <summary>彙總後數值。</summary>
    public decimal Amount { get; init; }
    /// <summary>營養素單位代碼。</summary>
    public string UnitCode { get; init; } = string.Empty;
}
