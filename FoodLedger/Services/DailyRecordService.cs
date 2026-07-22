using FoodLedger.DTOs.DailyRecords;
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

        if (request.ConsumedAt > _timeProvider.GetUtcNow())
        {
            throw new ArgumentOutOfRangeException(nameof(request.ConsumedAt));
        }

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
        };

        _dbContext.DailyRecords.Add(dailyRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
