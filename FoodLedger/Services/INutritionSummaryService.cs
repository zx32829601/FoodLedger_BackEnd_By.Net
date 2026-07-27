using FoodLedger.DTOs.Nutrition;

namespace FoodLedger.Services;

/// <summary>
/// 定義目前使用者的營養攝取彙總查詢。
/// </summary>
public interface INutritionSummaryService
{
    Task<DailyNutritionSummaryResponse> GetDailyAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);
}
