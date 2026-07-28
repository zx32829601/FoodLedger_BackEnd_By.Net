namespace FoodLedger.DTOs.Nutrition;

/// <summary>
/// 使用者指定本地日期的營養攝取總量。
/// </summary>
public sealed class DailyNutritionSummaryResponse
{
    /// <summary>彙總日期。</summary>
    public DateOnly Date { get; init; }

    /// <summary>用來切分本地日界的 IANA timezone。</summary>
    public string TimeZone { get; init; } = string.Empty;

    /// <summary>當日有資料的動態營養素總量。</summary>
    public IReadOnlyList<NutritionTotalResponse> Totals { get; init; } = [];

    /// <summary>當日依餐別拆分的動態營養素總量。</summary>
    public IReadOnlyList<MealTypeNutritionSummaryResponse> MealTypes { get; init; } = [];
}

/// <summary>
/// 單一餐別的營養素攝取總量。
/// </summary>
public sealed class MealTypeNutritionSummaryResponse
{
    /// <summary>餐別穩定代碼。</summary>
    public string MealTypeCode { get; init; } = string.Empty;

    /// <summary>此餐別有資料的動態營養素總量。</summary>
    public IReadOnlyList<NutritionTotalResponse> Totals { get; init; } = [];
}
