using FoodLedger.DTOs.DailyRecords;
using FoodLedger.Data.Entities;
using FoodLedger.Services;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Tests.Services;

/// <summary>
/// 驗證 <see cref="DailyRecordService" /> 的每日飲食紀錄商業規則。
/// </summary>
public partial class DailyRecordServiceTests
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
            MealTypeCode = "Lunch",
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
            MealTypeCode = "Snack",
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
            MealTypeCode = "Snack",
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
            MealTypeCode = "Snack",
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
            MealTypeCode = "Dinner",
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
        SeedSimpleFood(dbContext);
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
        var records = await service.GetDailyRecordsAsync(
            targetDate,
            "Etc/UTC",
            "zh-TW");

        // Assert
        var dailyRecord = records.Single();
        Assert.Multiple(() =>
        {
            Assert.That(dailyRecord.RecordId, Is.EqualTo(1));
            Assert.That(dailyRecord.FoodId, Is.EqualTo(1));
            Assert.That(dailyRecord.Food.DisplayName, Is.EqualTo("測試食物"));
            Assert.That(dailyRecord.Food.LangCode, Is.EqualTo("zh-TW"));
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
        var records = await service.GetDailyRecordsAsync(
            targetDate,
            "Etc/UTC",
            "zh-TW");

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
        var records = await service.GetDailyRecordsAsync(
            targetDate,
            "Etc/UTC",
            "zh-TW");

        // Assert
        Assert.That(records.Select(record => record.RecordId), Is.EqualTo(new[] { 2, 3, 1 }));
    }

    /// <summary>
    /// 驗證食用時間完全相同時，Service 會再依飲食紀錄識別碼由小到大回傳穩定順序。
    /// </summary>
    [Test]
    public async Task GetDailyRecordsAsync_WhenRecordsHaveSameConsumedAt_ReturnsRecordsOrderedByRecordId()
    {
        // Arrange
        var targetDate = new DateOnly(2026, 7, 23);
        var consumedAt = new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        dbContext.DailyRecords.AddRange(
            new DailyRecord
            {
                RecordId = 3,
                UserId = CurrentUserId,
                FoodId = 1,
                Quantity = 1,
                ConsumedAt = consumedAt,
            },
            new DailyRecord
            {
                RecordId = 1,
                UserId = CurrentUserId,
                FoodId = 1,
                Quantity = 1,
                ConsumedAt = consumedAt,
            },
            new DailyRecord
            {
                RecordId = 2,
                UserId = CurrentUserId,
                FoodId = 1,
                Quantity = 1,
                ConsumedAt = consumedAt,
            });
        await dbContext.SaveChangesAsync();
        var currentUserService = new TestCurrentUserService { UserId = CurrentUserId };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));

        // Act
        var records = await service.GetDailyRecordsAsync(
            targetDate,
            "Etc/UTC",
            "zh-TW");

        // Assert
        Assert.That(records.Select(record => record.RecordId), Is.EqualTo(new[] { 1, 2, 3 }));
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
            async () => await service.GetDailyRecordsAsync(
                new DateOnly(2026, 7, 23),
                "Etc/UTC",
                "zh-TW"));
    }

    /// <summary>
    /// 驗證刪除屬於目前登入使用者的飲食紀錄時，Service 會從資料庫移除該筆資料。
    /// </summary>
    [Test]
    public async Task DeleteDailyRecordAsync_WhenRecordBelongsToCurrentUser_RemovesRecord()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.DailyRecords.Add(new DailyRecord
        {
            RecordId = 1,
            UserId = CurrentUserId,
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero),
        });
        await dbContext.SaveChangesAsync();
        var currentUserService = new TestCurrentUserService { UserId = CurrentUserId };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));

        // Act
        await service.DeleteDailyRecordAsync(1);

        // Assert
        Assert.That(await dbContext.DailyRecords.AnyAsync(record => record.RecordId == 1), Is.False);
    }

    /// <summary>
    /// 驗證未登入使用者刪除飲食紀錄時，Service 會拒絕操作並保留既有資料。
    /// </summary>
    [Test]
    public async Task DeleteDailyRecordAsync_WhenCurrentUserIsMissing_ThrowsUnauthorizedAccessExceptionAndDoesNotDeleteRecord()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.DailyRecords.Add(new DailyRecord
        {
            RecordId = 1,
            UserId = CurrentUserId,
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero),
        });
        await dbContext.SaveChangesAsync();
        var currentUserService = new TestCurrentUserService { UserId = null };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await service.DeleteDailyRecordAsync(1));
        Assert.That(await dbContext.DailyRecords.AnyAsync(record => record.RecordId == 1), Is.True);
    }

    /// <summary>
    /// 驗證目前登入使用者嘗試刪除其他使用者的飲食紀錄時，Service 會使用找不到資料語意拒絕並保留資料。
    /// </summary>
    [Test]
    public async Task DeleteDailyRecordAsync_WhenRecordBelongsToAnotherUser_ThrowsKeyNotFoundExceptionAndDoesNotDeleteRecord()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.DailyRecords.Add(new DailyRecord
        {
            RecordId = 1,
            UserId = 99,
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero),
        });
        await dbContext.SaveChangesAsync();
        var currentUserService = new TestCurrentUserService { UserId = CurrentUserId };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await service.DeleteDailyRecordAsync(1));
        Assert.That(await dbContext.DailyRecords.AnyAsync(record => record.RecordId == 1), Is.True);
    }

    /// <summary>
    /// 驗證刪除不存在的飲食紀錄時，Service 會使用找不到資料語意拒絕並保留同使用者既有資料。
    /// </summary>
    [Test]
    public async Task DeleteDailyRecordAsync_WhenRecordDoesNotExist_ThrowsKeyNotFoundExceptionAndDoesNotDeleteOtherRecords()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.DailyRecords.Add(new DailyRecord
        {
            RecordId = 1,
            UserId = CurrentUserId,
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero),
        });
        await dbContext.SaveChangesAsync();
        var currentUserService = new TestCurrentUserService { UserId = CurrentUserId };
        var service = new DailyRecordService(dbContext, currentUserService, new TestTimeProvider(FixedUtcNow));

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await service.DeleteDailyRecordAsync(999));
        var originalRecordExists = await dbContext.DailyRecords.AnyAsync(record => record.RecordId == 1);
        var recordCount = await dbContext.DailyRecords.CountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(originalRecordExists, Is.True);
            Assert.That(recordCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// 驗證新增紀錄時會保存餐別，並將備註前後空白移除。
    /// </summary>
    [Test]
    public async Task CreateDailyRecordAsync_WhenMealTypeIsActive_SavesMealTypeAndTrimmedNote()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        SeedSimpleFood(dbContext);
        await dbContext.SaveChangesAsync();
        var service = new DailyRecordService(
            dbContext,
            new TestCurrentUserService { UserId = CurrentUserId },
            new TestTimeProvider(FixedUtcNow));
        var request = new CreateDailyRecordRequest
        {
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = FixedUtcNow,
            MealTypeCode = "Lunch",
            Note = "  公司午餐  ",
        };

        // Act
        await service.CreateDailyRecordAsync(request);

        // Assert
        var record = await dbContext.DailyRecords.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(record.MealTypeCode, Is.EqualTo("Lunch"));
            Assert.That(record.Note, Is.EqualTo("公司午餐"));
        });
    }

    /// <summary>
    /// 驗證新增紀錄使用停用餐別時，回報穩定欄位驗證錯誤且不寫入資料。
    /// </summary>
    [Test]
    public async Task CreateDailyRecordAsync_WhenMealTypeIsInactive_ThrowsValidationExceptionAndDoesNotWriteDatabase()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        SeedSimpleFood(dbContext);
        dbContext.DefinedCodes.Single(code =>
            code.CodeType == DefinedCodeTypes.MealType && code.Code == "Lunch").IsActive = false;
        await dbContext.SaveChangesAsync();
        var service = new DailyRecordService(
            dbContext,
            new TestCurrentUserService { UserId = CurrentUserId },
            new TestTimeProvider(FixedUtcNow));
        var request = new CreateDailyRecordRequest
        {
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = FixedUtcNow,
            MealTypeCode = "Lunch",
        };

        // Act
        var exception = Assert.ThrowsAsync<DailyRecordValidationException>(
            async () => await service.CreateDailyRecordAsync(request));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(exception?.FieldName, Is.EqualTo(nameof(CreateDailyRecordRequest.MealTypeCode)));
            Assert.That(exception?.ErrorCode, Is.EqualTo("DailyRecord.InvalidMealType"));
            Assert.That(dbContext.DailyRecords, Is.Empty);
        });
    }

    /// <summary>
    /// 驗證修改自己的紀錄時可更新所有核心欄位，並正規化 UTC 與空白備註。
    /// </summary>
    [Test]
    public async Task UpdateDailyRecordAsync_WhenRecordBelongsToCurrentUser_UpdatesCoreFields()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        SeedSimpleFood(dbContext);
        dbContext.SimpleFoods.Add(new SimpleFood { FoodId = 2, FoodCode = "UPDATED_FOOD" });
        dbContext.DailyRecords.Add(new DailyRecord
        {
            RecordId = 1,
            UserId = CurrentUserId,
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = FixedUtcNow.AddHours(-2),
            MealTypeCode = "Breakfast",
        });
        await dbContext.SaveChangesAsync();
        var service = new DailyRecordService(
            dbContext,
            new TestCurrentUserService { UserId = CurrentUserId },
            new TestTimeProvider(FixedUtcNow));
        var consumedAt = new DateTimeOffset(2026, 7, 21, 19, 0, 0, TimeSpan.FromHours(8));
        var request = new UpdateDailyRecordRequest
        {
            FoodId = 2,
            Quantity = 2.5m,
            ConsumedAt = consumedAt,
            MealTypeCode = "Dinner",
            Note = "   ",
        };

        // Act
        await service.UpdateDailyRecordAsync(1, request);

        // Assert
        var record = await dbContext.DailyRecords.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(record.FoodId, Is.EqualTo(2));
            Assert.That(record.Quantity, Is.EqualTo(2.5m));
            Assert.That(record.ConsumedAt, Is.EqualTo(consumedAt.ToUniversalTime()));
            Assert.That(record.MealTypeCode, Is.EqualTo("Dinner"));
            Assert.That(record.Note, Is.Null);
        });
    }

    /// <summary>
    /// 驗證修改其他使用者的紀錄時使用找不到語意，且不改變原始資料。
    /// </summary>
    [Test]
    public async Task UpdateDailyRecordAsync_WhenRecordBelongsToAnotherUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        SeedSimpleFood(dbContext);
        dbContext.DailyRecords.Add(new DailyRecord
        {
            RecordId = 1,
            UserId = 99,
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = FixedUtcNow,
            MealTypeCode = "Snack",
        });
        await dbContext.SaveChangesAsync();
        var service = new DailyRecordService(
            dbContext,
            new TestCurrentUserService { UserId = CurrentUserId },
            new TestTimeProvider(FixedUtcNow));
        var request = new UpdateDailyRecordRequest
        {
            FoodId = 1,
            Quantity = 2,
            ConsumedAt = FixedUtcNow,
            MealTypeCode = "Lunch",
        };

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await service.UpdateDailyRecordAsync(1, request));
        Assert.That((await dbContext.DailyRecords.SingleAsync()).Quantity, Is.EqualTo(1));
    }

    /// <summary>
    /// 驗證修改時使用停用餐別會回報穩定驗證錯誤，且保留原始紀錄。
    /// </summary>
    [Test]
    public async Task UpdateDailyRecordAsync_WhenMealTypeIsInactive_ThrowsValidationExceptionAndDoesNotUpdateRecord()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        SeedSimpleFood(dbContext);
        dbContext.DailyRecords.Add(new DailyRecord
        {
            RecordId = 1,
            UserId = CurrentUserId,
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = FixedUtcNow,
            MealTypeCode = "Snack",
        });
        dbContext.DefinedCodes.Single(code =>
            code.CodeType == DefinedCodeTypes.MealType && code.Code == "Lunch").IsActive = false;
        await dbContext.SaveChangesAsync();
        var service = new DailyRecordService(
            dbContext,
            new TestCurrentUserService { UserId = CurrentUserId },
            new TestTimeProvider(FixedUtcNow));
        var request = new UpdateDailyRecordRequest
        {
            FoodId = 1,
            Quantity = 2,
            ConsumedAt = FixedUtcNow,
            MealTypeCode = "Lunch",
        };

        // Act
        var exception = Assert.ThrowsAsync<DailyRecordValidationException>(
            async () => await service.UpdateDailyRecordAsync(1, request));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(exception?.ErrorCode, Is.EqualTo("DailyRecord.InvalidMealType"));
            Assert.That((dbContext.DailyRecords.Single()).Quantity, Is.EqualTo(1));
            Assert.That((dbContext.DailyRecords.Single()).MealTypeCode, Is.EqualTo("Snack"));
        });
    }

    private static ApplicationDbContext CreateDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? CreateDatabaseName())
            .Options;

        var dbContext = new ApplicationDbContext(options);
        dbContext.Database.EnsureCreated();
        return dbContext;
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
            Translations =
            [
                new SimpleFoodTranslation
                {
                    LangCode = "zh-TW",
                    FoodName = "測試食物",
                },
            ],
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
