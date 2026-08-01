using FoodLedger.DTOs.Nutrition;
using FoodLedger.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Services;

/// <summary>
/// 以每 100 克營養資料與實際克數計算目前使用者的營養攝取總量。
/// </summary>
public sealed class NutritionSummaryService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : INutritionSummaryService
{
    // Weekly summary 固定涵蓋週一至週日，共七個本地日。
    private const int DaysPerWeek = 7;

    /// <inheritdoc />
    public async Task<DailyNutritionSummaryResponse> GetDailyAsync(
        DateOnly date,
        string timeZone,
        string langCode,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        var (startAt, endAt) = LocalDateRange.GetUtcRange(
            date,
            date.AddDays(1),
            timeZoneInfo);
        var contributions = await LoadContributionsAsync(
            userId,
            startAt,
            endAt,
            langCode,
            cancellationToken);

        return new DailyNutritionSummaryResponse
        {
            Date = date,
            TimeZone = timeZoneInfo.Id,
            Totals = Aggregate(contributions),
            MealTypes = contributions
                .GroupBy(contribution => contribution.MealTypeCode)
                .OrderBy(group => group.Key)
                .Select(group => new MealTypeNutritionSummaryResponse
                {
                    MealTypeCode = group.Key,
                    Totals = Aggregate(group),
                })
                .ToArray(),
        };
    }

    /// <inheritdoc />
    public async Task<WeeklyNutritionSummaryResponse> GetWeeklyAsync(
        DateOnly focusDate,
        string timeZone,
        string langCode,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        var daysSinceMonday =
            ((int)focusDate.DayOfWeek + DaysPerWeek - 1) % DaysPerWeek;
        var startDate = focusDate.AddDays(-daysSinceMonday);
        var endDate = startDate.AddDays(DaysPerWeek - 1);
        var (startAt, endAt) = LocalDateRange.GetUtcRange(
            startDate,
            endDate.AddDays(1),
            timeZoneInfo);
        var contributions = await LoadContributionsAsync(
            userId,
            startAt,
            endAt,
            langCode,
            cancellationToken);
        var contributionsByDate = contributions
            .GroupBy(contribution => DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTime(contribution.ConsumedAt, timeZoneInfo).Date))
            .ToDictionary(group => group.Key, group => (IEnumerable<NutrientContribution>)group);

        return new WeeklyNutritionSummaryResponse
        {
            StartDate = startDate,
            EndDate = endDate,
            TimeZone = timeZoneInfo.Id,
            Totals = Aggregate(contributions),
            Days = Enumerable.Range(0, DaysPerWeek)
                .Select(offset =>
                {
                    var date = startDate.AddDays(offset);
                    return new DailyNutritionBreakdownResponse
                    {
                        Date = date,
                        Totals = Aggregate(contributionsByDate.GetValueOrDefault(date, [])),
                    };
                })
                .ToArray(),
        };
    }

    private async Task<IReadOnlyList<NutrientContribution>> LoadContributionsAsync(
        long userId,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        string langCode,
        CancellationToken cancellationToken)
    {
        var requestedLangCode = LocalizationRules.NormalizeLangCode(langCode);
        var fallbackLangCode = LocalizationRules.NormalizeLangCode(
            LocalizationRules.FallbackLangCode);
        var rows = await (
            from record in dbContext.DailyRecords
            join foodNutrient in dbContext.FoodNutrients on record.FoodId equals foodNutrient.FoodId
            where record.UserId == userId
                && record.ConsumedAt >= startAt
                && record.ConsumedAt < endAt
            select new
            {
                ConsumedAt = record.ConsumedAt,
                MealTypeCode = record.MealTypeCode,
                NutrientId = foodNutrient.NutrientId,
                Code = foodNutrient.Nutrient.NutrientCode,
                Translation = foodNutrient.Nutrient.Translations
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
                Amount = foodNutrient.Amount
                    * record.Quantity
                    / NutritionCalculationRules.BasisGrams,
                UnitCode = foodNutrient.Nutrient.UnitCode,
                DisplayOrder = foodNutrient.Nutrient.DisplayOrder,
            }).ToListAsync(cancellationToken);

        return rows
            .Select(row => new NutrientContribution
            {
                ConsumedAt = row.ConsumedAt,
                MealTypeCode = row.MealTypeCode,
                NutrientId = row.NutrientId,
                Code = row.Code,
                DisplayName = row.Translation?.Name,
                LangCode = row.Translation?.LangCode,
                Amount = row.Amount,
                UnitCode = row.UnitCode,
                DisplayOrder = row.DisplayOrder,
            })
            .ToArray();
    }

    private static IReadOnlyList<NutritionTotalResponse> Aggregate(
        IEnumerable<NutrientContribution> contributions)
    {
        return contributions
            .GroupBy(contribution => new
            {
                contribution.NutrientId,
                contribution.Code,
                contribution.DisplayName,
                contribution.LangCode,
                contribution.UnitCode,
                contribution.DisplayOrder,
            })
            .OrderBy(group => group.Key.DisplayOrder)
            .ThenBy(group => group.Key.Code)
            .Select(group => new NutritionTotalResponse
            {
                NutrientId = group.Key.NutrientId,
                Code = group.Key.Code,
                DisplayName = group.Key.DisplayName ?? group.Key.Code,
                LangCode = group.Key.LangCode,
                Amount = group.Sum(contribution => contribution.Amount),
                UnitCode = group.Key.UnitCode,
                DisplayOrder = group.Key.DisplayOrder,
            })
            .ToArray();
    }

    private sealed class NutrientContribution
    {
        public DateTimeOffset ConsumedAt { get; init; }
        public string MealTypeCode { get; init; } = string.Empty;
        public long NutrientId { get; init; }
        public string Code { get; init; } = string.Empty;
        public string? DisplayName { get; init; }
        public string? LangCode { get; init; }
        public decimal Amount { get; init; }
        public string UnitCode { get; init; } = string.Empty;
        public int DisplayOrder { get; init; }
    }
}
