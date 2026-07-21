using FoodLedger.DTOs.DailyRecords;
using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Services;

/// <summary>
/// 實作每日飲食紀錄相關商業邏輯。
/// </summary>
public sealed class DailyRecordService : IDailyRecordService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// 建立每日飲食紀錄服務。
    /// </summary>
    /// <param name="dbContext">應用程式資料庫內容。</param>
    /// <param name="currentUserService">目前登入使用者資訊來源。</param>
    public DailyRecordService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
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
            ConsumedAt = request.ConsumedAt,
        };

        _dbContext.DailyRecords.Add(dailyRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
