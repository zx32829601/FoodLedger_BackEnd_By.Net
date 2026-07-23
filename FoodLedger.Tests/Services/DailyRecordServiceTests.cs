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
    /// 驗證食物識別碼為 0 時，Service 會視為參數範圍錯誤並避免寫入每日紀錄。
    /// </summary>
    [Test]
    public async Task CreateDailyRecordAsync_WhenFoodIdIsZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var currentUserService = new TestCurrentUserService { UserId = CurrentUserId };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));
        var request = new CreateDailyRecordRequest
        {
            FoodId = 0,
            Quantity = 1,
            ConsumedAt = FixedUtcNow,
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await service.CreateDailyRecordAsync(request));
        Assert.That(exception?.ParamName, Is.EqualTo(nameof(CreateDailyRecordRequest.FoodId)));
        Assert.That(await dbContext.DailyRecords.CountAsync(), Is.EqualTo(0));
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
    /// 驗證餐點份量超過業務上限時，Service 會拒絕新增並維持資料庫不被寫入。
    /// </summary>
    [Test]
    public async Task CreateDailyRecordAsync_WhenQuantityExceedsBusinessMaximum_ThrowsArgumentOutOfRangeExceptionAndDoesNotWriteDatabase()
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
            Quantity = 10000.001m,
            ConsumedAt = FixedUtcNow,
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await service.CreateDailyRecordAsync(request));
        Assert.That(exception?.ParamName, Is.EqualTo(nameof(CreateDailyRecordRequest.Quantity)));
        Assert.That(await dbContext.DailyRecords.CountAsync(), Is.EqualTo(0));
    }

    /// <summary>
    /// 驗證餐點份量等於業務上限時，Service 會允許新增並持久化飲食紀錄。
    /// </summary>
    [Test]
    public async Task CreateDailyRecordAsync_WhenQuantityEqualsBusinessMaximum_CreatesDailyRecord()
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
            Quantity = 10000m,
            ConsumedAt = FixedUtcNow,
        };

        // Act
        await service.CreateDailyRecordAsync(request);

        // Assert
        await using var verificationDbContext = CreateDbContext(databaseName);
        var dailyRecord = await verificationDbContext.DailyRecords.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dailyRecord.UserId, Is.EqualTo(CurrentUserId));
            Assert.That(dailyRecord.Quantity, Is.EqualTo(request.Quantity));
        });
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

    /// <summary>
    /// 驗證非 UTC offset 且實際時間點晚於目前 UTC 時，Service 會拒絕新增並維持資料庫不被寫入。
    /// </summary>
    [Test]
    public async Task CreateDailyRecordAsync_WhenConsumedAtHasNonUtcOffsetAndInstantIsFuture_ThrowsArgumentOutOfRangeExceptionAndDoesNotWriteDatabase()
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
            ConsumedAt = new DateTimeOffset(2026, 7, 21, 20, 1, 0, TimeSpan.FromHours(8)),
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await service.CreateDailyRecordAsync(request));
        Assert.That(exception?.ParamName, Is.EqualTo(nameof(CreateDailyRecordRequest.ConsumedAt)));
        Assert.That(await dbContext.DailyRecords.CountAsync(), Is.EqualTo(0));
    }

    /// <summary>
    /// 驗證依日期查詢飲食紀錄時，Service 只會回傳目前登入使用者在指定 UTC 日期內的資料。
    /// </summary>
    [Test]
    public async Task GetDailyRecordsAsync_WhenCurrentUserHasRecordsOnDate_ReturnsOnlyThatUsersRecordsForDate()
    {
        // Arrange
        var targetDate = new DateOnly(2026, 7, 23);
        await using var dbContext = CreateDbContext();
        dbContext.DailyRecords.AddRange(
            new DailyRecord
            {
                RecordId = 1,
                UserId = CurrentUserId,
                FoodId = 1,
                Quantity = 1.5m,
                ConsumedAt = new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero),
            },
            new DailyRecord
            {
                RecordId = 2,
                UserId = 99,
                FoodId = 1,
                Quantity = 2,
                ConsumedAt = new DateTimeOffset(2026, 7, 23, 13, 0, 0, TimeSpan.Zero),
            },
            new DailyRecord
            {
                RecordId = 3,
                UserId = CurrentUserId,
                FoodId = 1,
                Quantity = 3,
                ConsumedAt = new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero),
            });
        await dbContext.SaveChangesAsync();
        var currentUserService = new TestCurrentUserService { UserId = CurrentUserId };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));

        // Act
        var records = await service.GetDailyRecordsAsync(targetDate);

        // Assert
        var dailyRecord = records.Single();
        Assert.Multiple(() =>
        {
            Assert.That(dailyRecord.RecordId, Is.EqualTo(1));
            Assert.That(dailyRecord.FoodId, Is.EqualTo(1));
            Assert.That(dailyRecord.Quantity, Is.EqualTo(1.5m));
            Assert.That(dailyRecord.ConsumedAt, Is.EqualTo(new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero)));
        });
    }

    /// <summary>
    /// 驗證依日期查詢飲食紀錄時，Service 會包含當日開始並排除隔日開始的邊界資料。
    /// </summary>
    [Test]
    public async Task GetDailyRecordsAsync_WhenRecordsAreOnDateBoundaries_IncludesStartAndExcludesNextDayStart()
    {
        // Arrange
        var targetDate = new DateOnly(2026, 7, 23);
        await using var dbContext = CreateDbContext();
        dbContext.DailyRecords.AddRange(
            new DailyRecord
            {
                RecordId = 1,
                UserId = CurrentUserId,
                FoodId = 1,
                Quantity = 1,
                ConsumedAt = new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero),
            },
            new DailyRecord
            {
                RecordId = 2,
                UserId = CurrentUserId,
                FoodId = 1,
                Quantity = 1,
                ConsumedAt = new DateTimeOffset(2026, 7, 23, 23, 59, 59, TimeSpan.Zero),
            },
            new DailyRecord
            {
                RecordId = 3,
                UserId = CurrentUserId,
                FoodId = 1,
                Quantity = 1,
                ConsumedAt = new DateTimeOffset(2026, 7, 24, 0, 0, 0, TimeSpan.Zero),
            });
        await dbContext.SaveChangesAsync();
        var currentUserService = new TestCurrentUserService { UserId = CurrentUserId };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));

        // Act
        var records = await service.GetDailyRecordsAsync(targetDate);

        // Assert
        Assert.That(records.Select(record => record.RecordId), Is.EqualTo(new[] { 1, 2 }));
    }

    /// <summary>
    /// 驗證同一天有多筆飲食紀錄時，Service 會依食用時間由早到晚回傳穩定順序。
    /// </summary>
    [Test]
    public async Task GetDailyRecordsAsync_WhenCurrentUserHasMultipleRecordsOnDate_ReturnsRecordsOrderedByConsumedAt()
    {
        // Arrange
        var targetDate = new DateOnly(2026, 7, 23);
        await using var dbContext = CreateDbContext();
        dbContext.DailyRecords.AddRange(
            new DailyRecord
            {
                RecordId = 1,
                UserId = CurrentUserId,
                FoodId = 1,
                Quantity = 1,
                ConsumedAt = new DateTimeOffset(2026, 7, 23, 18, 0, 0, TimeSpan.Zero),
            },
            new DailyRecord
            {
                RecordId = 2,
                UserId = CurrentUserId,
                FoodId = 1,
                Quantity = 1,
                ConsumedAt = new DateTimeOffset(2026, 7, 23, 8, 0, 0, TimeSpan.Zero),
            },
            new DailyRecord
            {
                RecordId = 3,
                UserId = CurrentUserId,
                FoodId = 1,
                Quantity = 1,
                ConsumedAt = new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero),
            });
        await dbContext.SaveChangesAsync();
        var currentUserService = new TestCurrentUserService { UserId = CurrentUserId };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));

        // Act
        var records = await service.GetDailyRecordsAsync(targetDate);

        // Assert
        Assert.That(records.Select(record => record.RecordId), Is.EqualTo(new[] { 2, 3, 1 }));
    }

    /// <summary>
    /// 驗證未登入使用者查詢飲食紀錄時，Service 會拒絕讀取私有資料。
    /// </summary>
    [Test]
    public void GetDailyRecordsAsync_WhenCurrentUserIsMissing_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var currentUserService = new TestCurrentUserService { UserId = null };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await service.GetDailyRecordsAsync(new DateOnly(2026, 7, 23)));
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
