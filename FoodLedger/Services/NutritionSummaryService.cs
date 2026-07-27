using FoodLedger.DTOs.Foods;
using FoodLedger.DTOs.Nutrition;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Services;

/// <summary>
/// 以每 100 克營養資料與實際克數計算目前使用者的每日攝取總量。
/// </summary>
public sealed class NutritionSummaryService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : INutritionSummaryService
{
    /// <inheritdoc />
    public async Task<DailyNutritionSummaryResponse> GetDailyAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var startAt = new DateTimeOffset(date, TimeOnly.MinValue, TimeSpan.Zero);
        var endAt = startAt.AddDays(1);
        var totals = await (
            from record in dbContext.DailyRecords
            join foodNutrient in dbContext.FoodNutrients on record.FoodId equals foodNutrient.FoodId
            where record.UserId == userId
                && record.ConsumedAt >= startAt
                && record.ConsumedAt < endAt
            group new { record, foodNutrient } by new
            {
                foodNutrient.NutrientId,
                foodNutrient.Nutrient.NutrientCode,
                foodNutrient.Nutrient.UnitCode,
            }
            into nutrientGroup
            orderby nutrientGroup.Key.NutrientCode
            select new NutritionTotalResponse
            {
                Code = nutrientGroup.Key.NutrientCode,
                DisplayName = nutrientGroup
                    .SelectMany(item => item.foodNutrient.Nutrient.Translations)
                    .Where(translation =>
                        translation.LangCode == FoodSearchRequest.DefaultLangCode
                        || translation.LangCode == FoodSearchRequest.FallbackLangCode)
                    .OrderBy(translation =>
                        translation.LangCode == FoodSearchRequest.DefaultLangCode ? 0 : 1)
                    .Select(translation => translation.NutrientName)
                    .FirstOrDefault() ?? nutrientGroup.Key.NutrientCode,
                Amount = nutrientGroup.Sum(item =>
                    item.foodNutrient.Amount * item.record.Quantity / 100m),
                UnitCode = nutrientGroup.Key.UnitCode,
            }).ToListAsync(cancellationToken);

        return new DailyNutritionSummaryResponse { Date = date, Totals = totals };
    }
}
