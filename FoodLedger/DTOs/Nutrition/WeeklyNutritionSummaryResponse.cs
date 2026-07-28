namespace FoodLedger.DTOs.Nutrition;

/// <summary>
/// 使用者指定焦點日期所在週的營養攝取總量。
/// </summary>
public sealed class WeeklyNutritionSummaryResponse
{
    /// <summary>週期起始日，固定為週一。</summary>
    public DateOnly StartDate { get; init; }

    /// <summary>週期結束日，固定為週日。</summary>
    public DateOnly EndDate { get; init; }

    /// <summary>用來切分本地日界的 IANA timezone。</summary>
    public string TimeZone { get; init; } = string.Empty;

    /// <summary>整週有資料的動態營養素總量。</summary>
    public IReadOnlyList<NutritionTotalResponse> Totals { get; init; } = [];

    /// <summary>固定包含週一至週日七天的每日 breakdown。</summary>
    public IReadOnlyList<DailyNutritionBreakdownResponse> Days { get; init; } = [];
}

/// <summary>
/// 週摘要中的單日營養素 breakdown。
/// </summary>
public sealed class DailyNutritionBreakdownResponse
{
    /// <summary>本地日曆日期。</summary>
    public DateOnly Date { get; init; }

    /// <summary>當日有資料的動態營養素總量。</summary>
    public IReadOnlyList<NutritionTotalResponse> Totals { get; init; } = [];
}
