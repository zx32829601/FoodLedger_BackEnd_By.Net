using FoodLedger.Data.Entities;
using FoodLedger.DTOs.DefinedCodes;
using FoodLedger.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Services;

/// <summary>
/// 使用資料庫通用代碼提供唯讀選項。
/// </summary>
public sealed class DefinedCodeService(ApplicationDbContext dbContext) : IDefinedCodeService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DefinedCodeResponse>> GetActiveMealTypesAsync(
        string langCode,
        CancellationToken cancellationToken = default)
    {
        return await GetActiveCodesAsync(
            DefinedCodeTypes.MealType,
            langCode,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DefinedCodeResponse>> GetActiveFitnessGoalsAsync(
        string langCode,
        CancellationToken cancellationToken = default)
    {
        return await GetActiveCodesAsync(
            DefinedCodeTypes.FitnessGoal,
            langCode,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DefinedCodeResponse>> GetActiveActivityLevelsAsync(
        string langCode,
        CancellationToken cancellationToken = default)
    {
        return await GetActiveCodesAsync(
            DefinedCodeTypes.ActivityLevel,
            langCode,
            cancellationToken);
    }

    private async Task<IReadOnlyList<DefinedCodeResponse>> GetActiveCodesAsync(
        string codeType,
        string langCode,
        CancellationToken cancellationToken)
    {
        var requestedLangCode = LocalizationRules.NormalizeLangCode(langCode);
        var fallbackLangCode = LocalizationRules.NormalizeLangCode(
            LocalizationRules.FallbackLangCode);
        var rows = await dbContext.DefinedCodes
            .AsNoTracking()
            .Where(code => code.CodeType == codeType && code.IsActive)
            .OrderBy(code => code.SortOrder)
            .ThenBy(code => code.Code)
            .Select(code => new
            {
                code.Code,
                code.SortOrder,
                Translation = code.Translations
                    .Where(translation =>
                        translation.LangCode.ToLower() == requestedLangCode
                        || translation.LangCode.ToLower() == fallbackLangCode)
                    .OrderBy(translation =>
                        translation.LangCode.ToLower() == requestedLangCode ? 0 : 1)
                    .Select(translation => new
                    {
                        translation.DisplayName,
                        translation.LangCode,
                        translation.Note,
                    })
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new DefinedCodeResponse
            {
                Code = row.Code,
                DisplayName = row.Translation?.DisplayName ?? row.Code,
                LangCode = row.Translation?.LangCode,
                Note = row.Translation?.Note,
                SortOrder = row.SortOrder,
            })
            .ToArray();
    }
}
