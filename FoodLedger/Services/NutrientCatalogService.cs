using FoodLedger.DTOs.Nutrition;
using FoodLedger.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Services;

/// <summary>
/// 使用指定語系與英文 fallback 讀取動態營養素目錄。
/// </summary>
public sealed class NutrientCatalogService(ApplicationDbContext dbContext)
    : INutrientCatalogService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<NutrientCatalogItemResponse>> GetAsync(
        string langCode,
        CancellationToken cancellationToken = default)
    {
        var requestedLangCode = LocalizationRules.NormalizeLangCode(langCode);
        var fallbackLangCode = LocalizationRules.NormalizeLangCode(
            LocalizationRules.FallbackLangCode);
        var rows = await dbContext.Nutrients
            .AsNoTracking()
            .OrderBy(nutrient => nutrient.DisplayOrder)
            .ThenBy(nutrient => nutrient.NutrientCode)
            .Select(nutrient => new
            {
                nutrient.NutrientId,
                Code = nutrient.NutrientCode,
                nutrient.UnitCode,
                nutrient.DisplayOrder,
                Translation = nutrient.Translations
                    .Where(translation =>
                        translation.LangCode.ToLower() == requestedLangCode
                        || translation.LangCode.ToLower() == fallbackLangCode)
                    .OrderBy(translation =>
                        translation.LangCode.ToLower() == requestedLangCode ? 0 : 1)
                    .Select(translation => new
                    {
                        Name = translation.NutrientName,
                        translation.LangCode,
                    })
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new NutrientCatalogItemResponse
            {
                NutrientId = row.NutrientId,
                Code = row.Code,
                DisplayName = row.Translation?.Name ?? row.Code,
                LangCode = row.Translation?.LangCode,
                UnitCode = row.UnitCode,
                DisplayOrder = row.DisplayOrder,
            })
            .ToArray();
    }
}
