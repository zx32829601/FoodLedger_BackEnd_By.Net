using FoodLedger.DTOs.Foods;
using FoodLedger.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Services;

/// <summary>提供輕量食物搜尋與完整食物明細。</summary>
public sealed class FoodSearchService(ApplicationDbContext dbContext) : IFoodSearchService
{
    private const string CaloriesCode = "Calories";

    /// <inheritdoc />
    public async Task<FoodSearchResponse> SearchAsync(
        FoodSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var queryText = request.Query?.Trim().ToLower() ?? string.Empty;
        var requestedLangCode = LocalizationRules.NormalizeLangCode(request.LangCode);
        var fallbackLangCode = LocalizationRules.NormalizeLangCode(LocalizationRules.FallbackLangCode);
        var foods = dbContext.SimpleFoods.AsNoTracking().Select(food => new
        {
            Food = food,
            Requested = food.Translations
                .Where(translation => translation.LangCode.ToLower() == requestedLangCode)
                .Select(translation => new { translation.FoodName, translation.LangCode })
                .FirstOrDefault(),
            English = food.Translations
                .Where(translation => translation.LangCode.ToLower() == fallbackLangCode)
                .Select(translation => new { translation.FoodName, translation.LangCode })
                .FirstOrDefault(),
        }).Where(item => item.Requested != null || item.English != null);

        if (queryText.Length > 0)
        {
            foods = foods.Where(item =>
                (item.Requested != null && item.Requested.FoodName.ToLower().Contains(queryText))
                || (item.English != null && item.English.FoodName.ToLower().Contains(queryText)));
        }

        var totalCount = await foods.CountAsync(cancellationToken);
        var rows = await foods
            .OrderBy(item => queryText.Length == 0 ? 0
                : item.Requested != null && item.Requested.FoodName.ToLower() == queryText ? 0
                : item.Requested != null && item.Requested.FoodName.ToLower().StartsWith(queryText) ? 1
                : item.Requested != null && item.Requested.FoodName.ToLower().Contains(queryText) ? 2
                : item.English != null && item.English.FoodName.ToLower() == queryText ? 3
                : item.English != null && item.English.FoodName.ToLower().StartsWith(queryText) ? 4
                : 5)
            .ThenBy(item => item.Requested != null ? item.Requested.FoodName : item.English!.FoodName)
            .ThenBy(item => item.Food.FoodId)
            .Skip((request.Page - FoodSearchRequest.MinimumPage) * request.PageSize)
            .Take(request.PageSize)
            .Select(item => new
            {
                item.Food.FoodId,
                item.Food.FoodCode,
                DisplayName = item.Requested != null ? item.Requested.FoodName : item.English!.FoodName,
                LangCode = item.Requested != null ? item.Requested.LangCode : item.English!.LangCode,
                EnglishName = item.English == null ? null : item.English.FoodName,
            })
            .ToListAsync(cancellationToken);

        var ids = rows.Select(row => row.FoodId).ToArray();
        var calories = await dbContext.FoodNutrients.AsNoTracking()
            .Where(item => ids.Contains(item.FoodId) && item.Nutrient.NutrientCode == CaloriesCode)
            .ToDictionaryAsync(item => item.FoodId, item => item.Amount, cancellationToken);

        return new FoodSearchResponse
        {
            Items = rows.Select(row => new FoodSearchItemResponse
            {
                FoodId = row.FoodId,
                FoodCode = row.FoodCode,
                DisplayName = row.DisplayName,
                LangCode = row.LangCode,
                EnglishName = row.LangCode.StartsWith("en", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(row.DisplayName, row.EnglishName, StringComparison.OrdinalIgnoreCase)
                    ? null
                    : row.EnglishName,
                CaloriesPer100Grams = calories.TryGetValue(row.FoodId, out var amount)
                    ? amount
                    : null,
            }).ToArray(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }

    /// <inheritdoc />
    public async Task<FoodDetailResponse?> GetAsync(
        long foodId,
        string langCode,
        CancellationToken cancellationToken = default)
    {
        var requestedLangCode = LocalizationRules.NormalizeLangCode(langCode);
        var fallbackLangCode = LocalizationRules.NormalizeLangCode(LocalizationRules.FallbackLangCode);
        var food = await dbContext.SimpleFoods.AsNoTracking()
            .Where(item => item.FoodId == foodId)
            .Select(item => new
            {
                item.FoodId,
                item.FoodCode,
                Translation = item.Translations
                    .Where(t => t.LangCode.ToLower() == requestedLangCode || t.LangCode.ToLower() == fallbackLangCode)
                    .OrderBy(t => t.LangCode.ToLower() == requestedLangCode ? 0 : 1)
                    .Select(t => new { t.FoodName, t.Description, t.LangCode })
                    .FirstOrDefault(),
                EnglishName = item.Translations
                    .Where(t => t.LangCode.ToLower() == fallbackLangCode)
                    .Select(t => t.FoodName)
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (food?.Translation is null) return null;

        var categories = await dbContext.SimpleFoodCategories.AsNoTracking()
            .Where(item => item.FoodId == foodId)
            .Select(item => new
            {
                item.CategoryId,
                Code = item.Category.CategoryCode,
                Translation = item.Category.Translations
                    .Where(t => t.LangCode.ToLower() == requestedLangCode || t.LangCode.ToLower() == fallbackLangCode)
                    .OrderBy(t => t.LangCode.ToLower() == requestedLangCode ? 0 : 1)
                    .Select(t => new { Name = t.CategoryName, t.LangCode })
                    .FirstOrDefault(),
            })
            .Where(item => item.Translation != null)
            .OrderBy(item => item.Translation!.Name)
            .ToListAsync(cancellationToken);
        var nutrients = await dbContext.FoodNutrients.AsNoTracking()
            .Where(item => item.FoodId == foodId)
            .OrderBy(item => item.Nutrient.DisplayOrder)
            .ThenBy(item => item.Nutrient.NutrientCode)
            .Select(item => new
            {
                Code = item.Nutrient.NutrientCode,
                item.Amount,
                item.Nutrient.UnitCode,
                item.Nutrient.DisplayOrder,
                Translation = item.Nutrient.Translations
                    .Where(t => t.LangCode.ToLower() == requestedLangCode || t.LangCode.ToLower() == fallbackLangCode)
                    .OrderBy(t => t.LangCode.ToLower() == requestedLangCode ? 0 : 1)
                    .Select(t => new { Name = t.NutrientName, t.LangCode })
                    .FirstOrDefault(),
            }).ToListAsync(cancellationToken);

        return new FoodDetailResponse
        {
            FoodId = food.FoodId,
            FoodCode = food.FoodCode,
            DisplayName = food.Translation.FoodName,
            LangCode = food.Translation.LangCode,
            EnglishName = food.Translation.LangCode.StartsWith("en", StringComparison.OrdinalIgnoreCase)
                || string.Equals(food.Translation.FoodName, food.EnglishName, StringComparison.OrdinalIgnoreCase)
                ? null
                : food.EnglishName,
            Description = string.IsNullOrWhiteSpace(food.Translation.Description) ? null : food.Translation.Description,
            Categories = categories.Select(item => new FoodCategoryResponse
            {
                CategoryId = item.CategoryId,
                Code = item.Code,
                DisplayName = item.Translation!.Name,
                LangCode = item.Translation.LangCode,
            }).ToArray(),
            Nutrients = nutrients.Select(item => new FoodNutrientResponse
            {
                Code = item.Code,
                DisplayName = item.Translation?.Name ?? item.Code,
                LangCode = item.Translation?.LangCode,
                DisplayOrder = item.DisplayOrder,
                AmountPer100Grams = item.Amount,
                UnitCode = item.UnitCode,
            }).ToArray(),
        };
    }
}
