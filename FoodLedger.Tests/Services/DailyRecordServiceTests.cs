using FoodLedger.DTOs.DailyRecords;
using FoodLedger.Services;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Tests.Services;

/// <summary>
/// 驗證 <see cref="DailyRecordService" /> 的每日飲食紀錄商業規則。
/// </summary>
public class DailyRecordServiceTests
{
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

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"DailyRecordServiceTests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public bool IsAuthenticated => UserId.HasValue;

        public long? UserId { get; init; }

        public string? UserName => null;
    }
}
