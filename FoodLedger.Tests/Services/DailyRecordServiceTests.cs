using FoodLedger.DTOs.DailyRecords;
using FoodLedger.Services;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Tests.Services;

/// <summary>
/// 驗證 <see cref="DailyRecordService" /> 的每日飲食紀錄商業規則。
/// </summary>
public class DailyRecordServiceTests
{
    // 測試用固定目前使用者 ID，用來確認新增紀錄的擁有者來自後端登入狀態。
    private const long CurrentUserId = 42;

    /// <summary>
    /// 驗證目前使用者不存在時，新增每日飲食紀錄會被拒絕且不寫入資料庫。
    /// </summary>
    [Test]
    public async Task CreateDailyRecordAsync_WhenCurrentUserIsMissing_ThrowsUnauthorizedAccessExceptionAndDoesNotWriteDatabase()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var currentUserService = new TestCurrentUserService { UserId = null };
        var service = new DailyRecordService(dbContext, currentUserService);
        var request = new CreateDailyRecordRequest
        {
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = DateTimeOffset.UtcNow,
        };

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await service.CreateDailyRecordAsync(request));
        Assert.That(await dbContext.DailyRecords.CountAsync(), Is.EqualTo(0));
    }

    /// <summary>
    /// 驗證目前使用者存在時，新增每日飲食紀錄會使用目前登入使用者作為紀錄擁有者並持久化。
    /// </summary>
    [Test]
    public async Task CreateDailyRecordAsync_WhenCurrentUserExists_CreatesDailyRecordForCurrentUser()
    {
        // Arrange
        var databaseName = CreateDatabaseName();
        var consumedAt = DateTimeOffset.UtcNow;
        await using var dbContext = CreateDbContext(databaseName);
        var currentUserService = new TestCurrentUserService { UserId = CurrentUserId };
        var service = new DailyRecordService(dbContext, currentUserService);
        var request = new CreateDailyRecordRequest
        {
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = consumedAt,
        };

        // Act
        await service.CreateDailyRecordAsync(request);

        // Assert
        await using var verificationDbContext = CreateDbContext(databaseName);
        var dailyRecord = await verificationDbContext.DailyRecords.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dailyRecord.UserId, Is.EqualTo(CurrentUserId));
            Assert.That(dailyRecord.FoodId, Is.EqualTo(request.FoodId));
            Assert.That(dailyRecord.Quantity, Is.EqualTo(request.Quantity));
            Assert.That(dailyRecord.ConsumedAt, Is.EqualTo(request.ConsumedAt));
        });
    }

    private static ApplicationDbContext CreateDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? CreateDatabaseName())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static string CreateDatabaseName()
    {
        return $"DailyRecordServiceTests-{Guid.NewGuid()}";
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public bool IsAuthenticated => UserId.HasValue;

        public long? UserId { get; init; }

        public string? UserName => null;
    }
}
