namespace FoodLedger.DTOs.Nutrition;

/// <summary>
/// 使用者指定 UTC 日期的營養攝取總量。
/// </summary>
public sealed class DailyNutritionSummaryResponse
{
    /// <summary>彙總日期。</summary>
    public DateOnly Date { get; init; }
    /// <summary>當日有資料的動態營養素總量。</summary>
    public IReadOnlyList<NutritionTotalResponse> Totals { get; init; } = [];
}
