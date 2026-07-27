using FoodLedger.DTOs.DailyRecords;
using FoodLedger.DTOs.Errors;
using FoodLedger.Data.Entities;
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

        if (request.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Quantity));
        }

        if (request.Quantity > MaximumQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Quantity));
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
            Quantity = request.Quantity,
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
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.UserId is not { } currentUserId)
        {
            throw new UnauthorizedAccessException();
        }

        var startAt = new DateTimeOffset(date, TimeOnly.MinValue, TimeSpan.Zero);
        var endAt = startAt.AddDays(1);

        return await _dbContext.DailyRecords
            .Where(record =>
                record.UserId == currentUserId
                && record.ConsumedAt >= startAt
                && record.ConsumedAt < endAt)
            .OrderBy(record => record.ConsumedAt)
            .ThenBy(record => record.RecordId)
            .Select(record => new DailyRecordResponse
            {
                RecordId = record.RecordId,
                FoodId = record.FoodId,
                Quantity = record.Quantity,
                ConsumedAt = record.ConsumedAt,
                MealTypeCode = record.MealTypeCode,
                Note = record.Note,
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

        if (request.Quantity <= 0 || request.Quantity > MaximumQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Quantity));
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
        dailyRecord.Quantity = request.Quantity;
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
