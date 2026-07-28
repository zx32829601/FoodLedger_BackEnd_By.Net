using FoodLedger.DTOs.Foods;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Services;

/// <summary>
/// 使用 EF Core 查詢食物翻譯與營養資料。
/// </summary>
public sealed class FoodSearchService(ApplicationDbContext dbContext) : IFoodSearchService
{
    /// <inheritdoc />
    public async Task<FoodSearchResponse> SearchAsync(
        FoodSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var queryText = request.Query?.Trim() ?? string.Empty;
        var requestedLangCode = request.LangCode.ToLowerInvariant();
        var fallbackLangCode = FoodSearchRequest.FallbackLangCode.ToLowerInvariant();
        var foods = dbContext.SimpleFoods
            .AsNoTracking()
            .Select(food => new
            {
                Food = food,
                Translation = food.Translations
                    .Where(translation =>
                        translation.LangCode.ToLower() == requestedLangCode
                        || translation.LangCode.ToLower() == fallbackLangCode)
                    .OrderBy(translation =>
                        translation.LangCode.ToLower() == requestedLangCode ? 0 : 1)
                    .Select(translation => new
                    {
                        translation.FoodName,
                        translation.LangCode,
                    })
                    .FirstOrDefault(),
            })
            .Where(item => item.Translation != null);

        if (!string.IsNullOrEmpty(queryText))
        {
            foods = foods.Where(item => item.Translation!.FoodName.Contains(queryText));
        }

        var totalCount = await foods.CountAsync(cancellationToken);
        var foodRows = await foods
            .OrderBy(item => item.Translation!.FoodName)
            .ThenBy(item => item.Food.FoodId)
            .Skip((request.Page - FoodSearchRequest.MinimumPage) * request.PageSize)
            .Take(request.PageSize)
            .Select(item => new
            {
                FoodId = item.Food.FoodId,
                FoodCode = item.Food.FoodCode,
                DisplayName = item.Translation!.FoodName,
                LangCode = item.Translation.LangCode,
            })
            .ToListAsync(cancellationToken);

        var foodIds = foodRows.Select(food => food.FoodId).ToArray();
        var nutrientRows = await dbContext.FoodNutrients
            .AsNoTracking()
            .Where(foodNutrient => foodIds.Contains(foodNutrient.FoodId))
            .Select(foodNutrient => new
            {
                foodNutrient.FoodId,
                Code = foodNutrient.Nutrient.NutrientCode,
                foodNutrient.Amount,
                foodNutrient.Nutrient.UnitCode,
                Translation = foodNutrient.Nutrient.Translations
                    .Where(translation =>
                        translation.LangCode.ToLower() == requestedLangCode
                        || translation.LangCode.ToLower() == fallbackLangCode)
                    .OrderBy(translation =>
                        translation.LangCode.ToLower() == requestedLangCode ? 0 : 1)
                    .Select(translation => translation.NutrientName)
                    .FirstOrDefault(),
            })
            .Where(nutrient => nutrient.Translation != null)
            .OrderBy(nutrient => nutrient.Code)
            .ToListAsync(cancellationToken);
        var nutrientsByFoodId = nutrientRows
            .GroupBy(nutrient => nutrient.FoodId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<FoodNutrientResponse>)group
                    .Select(nutrient => new FoodNutrientResponse
                    {
                        Code = nutrient.Code,
                        DisplayName = nutrient.Translation!,
                        AmountPer100Grams = nutrient.Amount,
                        UnitCode = nutrient.UnitCode,
                    })
                    .ToArray());
        var items = foodRows
            .Select(food => new FoodSearchItemResponse
            {
                FoodId = food.FoodId,
                FoodCode = food.FoodCode,
                DisplayName = food.DisplayName,
                LangCode = food.LangCode,
                Nutrients = nutrientsByFoodId.GetValueOrDefault(food.FoodId, []),
            })
            .ToArray();

        return new FoodSearchResponse
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }

}
