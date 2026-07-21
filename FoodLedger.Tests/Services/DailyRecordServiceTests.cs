using FoodLedger.DTOs.DailyRecords;
using FoodLedger.Data.Entities;
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
    private static readonly DateTimeOffset FixedUtcNow = new(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// 驗證目前使用者不存在時，新增每日飲食紀錄會被拒絕且不寫入資料庫。
    /// </summary>
    [Test]
    public async Task CreateDailyRecordAsync_WhenCurrentUserIsMissing_ThrowsUnauthorizedAccessExceptionAndDoesNotWriteDatabase()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var currentUserService = new TestCurrentUserService { UserId = null };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));
        var request = new CreateDailyRecordRequest
        {
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = FixedUtcNow,
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
        var consumedAt = FixedUtcNow;
        await using var dbContext = CreateDbContext(databaseName);
        SeedSimpleFood(dbContext);
        await dbContext.SaveChangesAsync();
        var currentUserService = new TestCurrentUserService { UserId = CurrentUserId };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));
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

    /// <summary>
    /// 驗證餐點份量為 0 或負數時，Service 會拒絕新增並維持資料庫不被寫入。
    /// </summary>
    [TestCase(0)]
    [TestCase(-1)]
    public async Task CreateDailyRecordAsync_WhenQuantityIsZeroOrNegative_ThrowsArgumentOutOfRangeExceptionAndDoesNotWriteDatabase(
        decimal quantity)
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var currentUserService = new TestCurrentUserService { UserId = CurrentUserId };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));
        var request = new CreateDailyRecordRequest
        {
            FoodId = 1,
            Quantity = quantity,
            ConsumedAt = FixedUtcNow,
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await service.CreateDailyRecordAsync(request));
        Assert.That(exception?.ParamName, Is.EqualTo(nameof(CreateDailyRecordRequest.Quantity)));
        Assert.That(await dbContext.DailyRecords.CountAsync(), Is.EqualTo(0));
    }

    /// <summary>
    /// 驗證指定的食物不存在時，Service 會拒絕新增並維持資料庫不被寫入。
    /// </summary>
    [Test]
    public async Task CreateDailyRecordAsync_WhenFoodDoesNotExist_ThrowsKeyNotFoundExceptionAndDoesNotWriteDatabase()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var currentUserService = new TestCurrentUserService { UserId = CurrentUserId };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));
        var request = new CreateDailyRecordRequest
        {
            FoodId = 999,
            Quantity = 1,
            ConsumedAt = FixedUtcNow,
        };

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await service.CreateDailyRecordAsync(request));
        Assert.That(await dbContext.DailyRecords.CountAsync(), Is.EqualTo(0));
    }

    /// <summary>
    /// 驗證用餐時間晚於目前 UTC 時間時，Service 會拒絕新增並維持資料庫不被寫入。
    /// </summary>
    [Test]
    public async Task CreateDailyRecordAsync_WhenConsumedAtIsInFuture_ThrowsArgumentOutOfRangeExceptionAndDoesNotWriteDatabase()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        SeedSimpleFood(dbContext);
        await dbContext.SaveChangesAsync();
        var currentUserService = new TestCurrentUserService { UserId = CurrentUserId };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));
        var request = new CreateDailyRecordRequest
        {
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = FixedUtcNow.AddMinutes(1),
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await service.CreateDailyRecordAsync(request));
        Assert.That(exception?.ParamName, Is.EqualTo(nameof(CreateDailyRecordRequest.ConsumedAt)));
        Assert.That(await dbContext.DailyRecords.CountAsync(), Is.EqualTo(0));
    }

    /// <summary>
    /// 驗證用餐時間等於目前 UTC 時間時，Service 會允許新增並持久化飲食紀錄。
    /// </summary>
    [Test]
    public async Task CreateDailyRecordAsync_WhenConsumedAtEqualsCurrentUtcTime_CreatesDailyRecord()
    {
        // Arrange
        var databaseName = CreateDatabaseName();
        await using var dbContext = CreateDbContext(databaseName);
        SeedSimpleFood(dbContext);
        await dbContext.SaveChangesAsync();
        var currentUserService = new TestCurrentUserService { UserId = CurrentUserId };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));
        var request = new CreateDailyRecordRequest
        {
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = FixedUtcNow,
        };

        // Act
        await service.CreateDailyRecordAsync(request);

        // Assert
        await using var verificationDbContext = CreateDbContext(databaseName);
        var dailyRecord = await verificationDbContext.DailyRecords.SingleAsync();
        Assert.That(dailyRecord.ConsumedAt, Is.EqualTo(FixedUtcNow));
    }

    /// <summary>
    /// 驗證非 UTC offset 但實際時間點未晚於目前 UTC 時，Service 會允許新增並以 UTC 時間持久化。
    /// </summary>
    [Test]
    public async Task CreateDailyRecordAsync_WhenConsumedAtHasNonUtcOffsetButInstantIsNotFuture_CreatesDailyRecordWithUtcConsumedAt()
    {
        // Arrange
        var databaseName = CreateDatabaseName();
        await using var dbContext = CreateDbContext(databaseName);
        SeedSimpleFood(dbContext);
        await dbContext.SaveChangesAsync();
        var currentUserService = new TestCurrentUserService { UserId = CurrentUserId };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));
        var consumedAt = new DateTimeOffset(2026, 7, 21, 20, 0, 0, TimeSpan.FromHours(8));
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
            Assert.That(dailyRecord.ConsumedAt, Is.EqualTo(consumedAt.ToUniversalTime()));
            Assert.That(dailyRecord.ConsumedAt.Offset, Is.EqualTo(TimeSpan.Zero));
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

    private static void SeedSimpleFood(ApplicationDbContext dbContext)
    {
        dbContext.SimpleFoods.Add(new SimpleFood
        {
            FoodId = 1,
            FoodCode = "TEST_FOOD",
        });
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public bool IsAuthenticated => UserId.HasValue;

        public long? UserId { get; init; }

        public string? UserName => null;
    }

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
