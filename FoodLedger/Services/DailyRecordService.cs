using FoodLedger.DTOs.DailyRecords;
using FoodLedger.DTOs.Errors;
using FoodLedger.Data.Entities;
using FoodLedger.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Services;

/// <summary>
/// 實作每日飲食紀錄相關商業邏輯。
/// </summary>
public sealed class DailyRecordService : IDailyRecordService
{
    private const decimal MaximumQuantity = 10000m;

    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// 建立每日飲食紀錄服務。
    /// </summary>
    /// <param name="dbContext">應用程式資料庫內容。</param>
    /// <param name="currentUserService">目前登入使用者資訊來源。</param>
    /// <param name="timeProvider">目前 UTC 時間來源，用於驗證飲食紀錄時間不可晚於現在。</param>
    public DailyRecordService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task CreateDailyRecordAsync(
        CreateDailyRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.UserId is not { } currentUserId)
        {
            throw new UnauthorizedAccessException();
        }

        if (request.QuantityInGrams <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.QuantityInGrams));
        }

        if (request.QuantityInGrams > MaximumQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(request.QuantityInGrams));
        }

        if (request.FoodId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.FoodId));
        }

        if (request.ConsumedAt > _timeProvider.GetUtcNow())
        {
            throw new ArgumentOutOfRangeException(nameof(request.ConsumedAt));
        }

        await ValidateMealTypeAsync(request.MealTypeCode, cancellationToken);

        var foodExists = await _dbContext.SimpleFoods
            .AnyAsync(food => food.FoodId == request.FoodId, cancellationToken);
        if (!foodExists)
        {
            throw new KeyNotFoundException($"Food {request.FoodId} does not exist.");
        }

        var dailyRecord = new DailyRecord
        {
            UserId = currentUserId,
            FoodId = request.FoodId,
            Quantity = request.QuantityInGrams,
            ConsumedAt = request.ConsumedAt.ToUniversalTime(),
            MealTypeCode = request.MealTypeCode,
            Note = NormalizeNote(request.Note),
        };

        _dbContext.DailyRecords.Add(dailyRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DailyRecordResponse>> GetDailyRecordsAsync(
        DateOnly date,
        string timeZone,
        string langCode,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.UserId is not { } currentUserId)
        {
            throw new UnauthorizedAccessException();
        }

        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        var (startAt, endAt) = LocalDateRange.GetUtcRange(
            date,
            date.AddDays(1),
            timeZoneInfo);
        var requestedLangCode = LocalizationRules.NormalizeLangCode(langCode);
        var fallbackLangCode = LocalizationRules.NormalizeLangCode(
            LocalizationRules.FallbackLangCode);

        return await _dbContext.DailyRecords
            .Where(record =>
                record.UserId == currentUserId
                && record.ConsumedAt >= startAt
                && record.ConsumedAt < endAt)
            .OrderBy(record => record.ConsumedAt)
            .ThenBy(record => record.RecordId)
            .Select(record => new
            {
                Record = record,
                FoodCode = _dbContext.SimpleFoods
                    .Where(food => food.FoodId == record.FoodId)
                    .Select(food => food.FoodCode)
                    .FirstOrDefault(),
                FoodTranslation = _dbContext.SimpleFoodTranslations
                    .Where(translation =>
                        translation.FoodId == record.FoodId
                        && (translation.LangCode.ToLower() == requestedLangCode
                            || translation.LangCode.ToLower() == fallbackLangCode))
                    .OrderBy(translation =>
                        translation.LangCode.ToLower() == requestedLangCode ? 0 : 1)
                    .Select(translation => new
                    {
                        Name = translation.FoodName,
                        translation.LangCode,
                    })
                    .FirstOrDefault(),
            })
            .Select(localizedRecord => new DailyRecordResponse
            {
                RecordId = localizedRecord.Record.RecordId,
                FoodId = localizedRecord.Record.FoodId,
                Food = new DailyRecordFoodResponse
                {
                    FoodId = localizedRecord.Record.FoodId,
                    FoodCode = localizedRecord.FoodCode ?? string.Empty,
                    DisplayName = localizedRecord.FoodTranslation == null
                        ? string.Empty
                        : localizedRecord.FoodTranslation.Name,
                    LangCode = localizedRecord.FoodTranslation == null
                        ? string.Empty
                        : localizedRecord.FoodTranslation.LangCode,
                },
                Nutrients = _dbContext.FoodNutrients
                    .Where(nutrient => nutrient.FoodId == localizedRecord.Record.FoodId)
                    .OrderBy(nutrient => nutrient.Nutrient.NutrientCode)
                    .Select(nutrient => new
                    {
                        FoodNutrient = nutrient,
                        Translation = nutrient.Nutrient.Translations
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
                    .Select(localizedNutrient => new DailyRecordNutrientResponse
                    {
                        NutrientId = localizedNutrient.FoodNutrient.NutrientId,
                        Code = localizedNutrient.FoodNutrient.Nutrient.NutrientCode,
                        DisplayName = localizedNutrient.Translation == null
                            ? localizedNutrient.FoodNutrient.Nutrient.NutrientCode
                            : localizedNutrient.Translation.Name,
                        LangCode = localizedNutrient.Translation == null
                            ? null
                            : localizedNutrient.Translation.LangCode,
                        Amount = localizedNutrient.FoodNutrient.Amount
                            * localizedRecord.Record.Quantity
                            / NutritionCalculationRules.BasisGrams,
                        UnitCode = localizedNutrient.FoodNutrient.Nutrient.UnitCode,
                    })
                    .ToArray(),
                QuantityInGrams = localizedRecord.Record.Quantity,
                ConsumedAt = localizedRecord.Record.ConsumedAt,
                MealTypeCode = localizedRecord.Record.MealTypeCode,
                Note = localizedRecord.Record.Note,
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateDailyRecordAsync(
        long recordId,
        UpdateDailyRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.UserId is not { } currentUserId)
        {
            throw new UnauthorizedAccessException();
        }

        if (request.QuantityInGrams <= 0 || request.QuantityInGrams > MaximumQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(request.QuantityInGrams));
        }

        if (request.FoodId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.FoodId));
        }

        if (request.ConsumedAt > _timeProvider.GetUtcNow())
        {
            throw new ArgumentOutOfRangeException(nameof(request.ConsumedAt));
        }

        var dailyRecord = await _dbContext.DailyRecords
            .FirstOrDefaultAsync(record =>
                record.RecordId == recordId && record.UserId == currentUserId,
                cancellationToken);
        if (dailyRecord is null)
        {
            throw new KeyNotFoundException($"DailyRecord {recordId} does not exist.");
        }

        var foodExists = await _dbContext.SimpleFoods
            .AnyAsync(food => food.FoodId == request.FoodId, cancellationToken);
        if (!foodExists)
        {
            throw new KeyNotFoundException($"Food {request.FoodId} does not exist.");
        }

        await ValidateMealTypeAsync(request.MealTypeCode, cancellationToken);

        dailyRecord.FoodId = request.FoodId;
        dailyRecord.Quantity = request.QuantityInGrams;
        dailyRecord.ConsumedAt = request.ConsumedAt.ToUniversalTime();
        dailyRecord.MealTypeCode = request.MealTypeCode;
        dailyRecord.Note = NormalizeNote(request.Note);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteDailyRecordAsync(
        long recordId,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.UserId is not { } currentUserId)
        {
            throw new UnauthorizedAccessException();
        }

        var dailyRecord = await _dbContext.DailyRecords
            .FirstOrDefaultAsync(record =>
                record.RecordId == recordId
                && record.UserId == currentUserId,
                cancellationToken);
        if (dailyRecord is null)
        {
            throw new KeyNotFoundException($"DailyRecord {recordId} does not exist.");
        }

        _dbContext.DailyRecords.Remove(dailyRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateMealTypeAsync(
        string mealTypeCode,
        CancellationToken cancellationToken)
    {
        var isValid = !string.IsNullOrWhiteSpace(mealTypeCode)
            && await _dbContext.DefinedCodes.AnyAsync(code =>
                code.CodeType == DefinedCodeTypes.MealType
                && code.Code == mealTypeCode
                && code.IsActive,
                cancellationToken);
        if (!isValid)
        {
            throw new DailyRecordValidationException(
                nameof(CreateDailyRecordRequest.MealTypeCode),
                DailyRecordErrorCodes.InvalidMealType);
        }
    }

    private static string? NormalizeNote(string? note)
    {
        var normalizedNote = note?.Trim();
        return string.IsNullOrEmpty(normalizedNote) ? null : normalizedNote;
    }
}
