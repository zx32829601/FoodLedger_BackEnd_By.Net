using FoodLedger.Data.Entities;
using FoodLedger.DTOs.BodyMeasurements;
using FoodLedger.DTOs.Errors;
using FoodLedger.Services;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Tests.Services;

/// <summary>驗證身體測量的所有權、輸入規則、歷史查詢與安全刪除流程。</summary>
[Category("BodyMeasurements")]
[Category("Unit")]
public sealed class BodyMeasurementServiceTests
{
    private const long CurrentUserId = 42;
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task CreateAsync_WithValidValues_UsesServerTimeAndCurrentUser()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(new CreateBodyMeasurementRequest
        {
            WeightInKilograms = 72.35m,
            BodyFatPercentage = 18.4m,
            MuscleMassInKilograms = 31.25m,
        });

        var entity = await dbContext.BodyMeasurements.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(entity.UserId, Is.EqualTo(CurrentUserId));
            Assert.That(entity.MeasuredAt, Is.EqualTo(FixedUtcNow));
            Assert.That(result.MeasuredAt, Is.EqualTo(FixedUtcNow));
            Assert.That(result.Version, Is.Not.EqualTo(Guid.Empty));
        });
    }

    [TestCase(19.99, BodyMeasurementErrorCodes.WeightOutOfRange)]
    [TestCase(400.01, BodyMeasurementErrorCodes.WeightOutOfRange)]
    [TestCase(72.123, BodyMeasurementErrorCodes.PrecisionExceeded)]
    public void CreateAsync_WithInvalidWeight_ReturnsStableErrorCode(
        decimal weight,
        string expectedCode)
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var exception = Assert.ThrowsAsync<BodyMeasurementValidationException>(
            async () => await service.CreateAsync(new CreateBodyMeasurementRequest
            {
                WeightInKilograms = weight,
            }));

        Assert.That(exception?.ErrorCode, Is.EqualTo(expectedCode));
    }

    [TestCase(1.99)]
    [TestCase(70.01)]
    public void CreateAsync_WithBodyFatOutsideRange_RejectsValue(decimal bodyFat)
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var exception = Assert.ThrowsAsync<BodyMeasurementValidationException>(
            async () => await service.CreateAsync(new CreateBodyMeasurementRequest
            {
                WeightInKilograms = 70m,
                BodyFatPercentage = bodyFat,
            }));

        Assert.That(exception?.ErrorCode,
            Is.EqualTo(BodyMeasurementErrorCodes.BodyFatOutOfRange));
    }

    [TestCase(0)]
    [TestCase(70.01)]
    public void CreateAsync_WithInvalidMuscleMass_RejectsValue(decimal muscleMass)
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var exception = Assert.ThrowsAsync<BodyMeasurementValidationException>(
            async () => await service.CreateAsync(new CreateBodyMeasurementRequest
            {
                WeightInKilograms = 70m,
                MuscleMassInKilograms = muscleMass,
            }));

        Assert.That(exception?.ErrorCode,
            Is.EqualTo(BodyMeasurementErrorCodes.MuscleMassOutOfRange));
    }

    [Test]
    public async Task CreateAsync_WithInclusiveBoundariesAndNullOptionals_AcceptsValues()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var minimum = await service.CreateAsync(new CreateBodyMeasurementRequest
        {
            WeightInKilograms = 20m,
            BodyFatPercentage = 2m,
        });
        var maximum = await service.CreateAsync(new CreateBodyMeasurementRequest
        {
            WeightInKilograms = 400m,
            BodyFatPercentage = 70m,
            MuscleMassInKilograms = 400m,
        });

        Assert.Multiple(() =>
        {
            Assert.That(minimum.MuscleMassInKilograms, Is.Null);
            Assert.That(maximum.WeightInKilograms, Is.EqualTo(400m));
            Assert.That(maximum.BodyFatPercentage, Is.EqualTo(70m));
        });
    }

    [Test]
    public async Task UpdateAsync_WithCurrentVersion_PreservesMeasuredAtAndRotatesVersion()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var created = await service.CreateAsync(new CreateBodyMeasurementRequest
        {
            WeightInKilograms = 70m,
        });

        var updated = await service.UpdateAsync(created.MeasurementId, new UpdateBodyMeasurementRequest
        {
            WeightInKilograms = 71m,
            Version = created.Version,
        });

        Assert.Multiple(() =>
        {
            Assert.That(updated.WeightInKilograms, Is.EqualTo(71m));
            Assert.That(updated.MeasuredAt, Is.EqualTo(created.MeasuredAt));
            Assert.That(updated.Version, Is.Not.EqualTo(created.Version));
        });
    }

    [Test]
    public async Task UpdateAsync_WithStaleVersion_ThrowsConflictAndKeepsValue()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var created = await service.CreateAsync(new CreateBodyMeasurementRequest
        {
            WeightInKilograms = 70m,
        });

        Assert.ThrowsAsync<BodyMeasurementConflictException>(async () =>
            await service.UpdateAsync(created.MeasurementId, new UpdateBodyMeasurementRequest
            {
                WeightInKilograms = 80m,
                Version = Guid.NewGuid(),
            }));

        Assert.That((await dbContext.BodyMeasurements.SingleAsync()).WeightInKilograms,
            Is.EqualTo(70m));
    }

    [Test]
    public void UpdateAsync_ForAnotherUser_MasksRecordAsNotFound()
    {
        using var dbContext = CreateDbContext();
        dbContext.BodyMeasurements.Add(CreateMeasurement(1, userId: 99));
        dbContext.SaveChanges();
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await service.UpdateAsync(1, new UpdateBodyMeasurementRequest
            {
                WeightInKilograms = 70m,
                Version = dbContext.BodyMeasurements.Single().Version,
            }));
    }

    [Test]
    public async Task GetHistoryAsync_ReturnsOwnedRecordsInStableDescendingPages()
    {
        await using var dbContext = CreateDbContext();
        var measuredAt = new DateTimeOffset(2026, 8, 20, 0, 0, 0, TimeSpan.Zero);
        dbContext.BodyMeasurements.AddRange(
            CreateMeasurement(1, CurrentUserId, measuredAt),
            CreateMeasurement(2, CurrentUserId, measuredAt),
            CreateMeasurement(3, CurrentUserId, measuredAt.AddDays(-1)),
            CreateMeasurement(4, 99, measuredAt.AddDays(1)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var firstPage = await service.GetHistoryAsync(new BodyMeasurementQueryRequest
        {
            Page = 1,
            PageSize = 2,
        });
        var secondPage = await service.GetHistoryAsync(new BodyMeasurementQueryRequest
        {
            Page = 2,
            PageSize = 2,
        });

        Assert.Multiple(() =>
        {
            Assert.That(firstPage.TotalCount, Is.EqualTo(3));
            Assert.That(firstPage.Items.Select(item => item.MeasurementId), Is.EqualTo([2, 1]));
            Assert.That(secondPage.Items.Select(item => item.MeasurementId), Is.EqualTo([3]));
        });
    }

    [Test]
    public async Task GetHistoryAsync_WithLocalDate_UsesProfilesIanaTimeZone()
    {
        await using var dbContext = CreateDbContext();
        dbContext.BodyProfiles.Add(CreateProfile());
        dbContext.BodyMeasurements.AddRange(
            CreateMeasurement(1, CurrentUserId,
                new DateTimeOffset(2026, 8, 1, 15, 59, 0, TimeSpan.Zero)),
            CreateMeasurement(2, CurrentUserId,
                new DateTimeOffset(2026, 8, 1, 16, 0, 0, TimeSpan.Zero)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetHistoryAsync(new BodyMeasurementQueryRequest
        {
            FromDate = new DateOnly(2026, 8, 2),
            ToDate = new DateOnly(2026, 8, 2),
        });

        Assert.That(result.Items.Select(item => item.MeasurementId), Is.EqualTo([2]));
    }

    [Test]
    public void GetHistoryAsync_WithLocalDateAndNoProfile_RequiresProfile()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<BodyMeasurementProfileRequiredException>(async () =>
            await service.GetHistoryAsync(new BodyMeasurementQueryRequest
            {
                FromDate = new DateOnly(2026, 8, 1),
            }));
    }

    [Test]
    public async Task DeletionImpactAndDelete_WithMatchingToken_DeletesMeasurement()
    {
        await using var dbContext = CreateDbContext();
        var tokenService = new TestImpactTokenService();
        var service = CreateService(dbContext, tokenService);
        var created = await service.CreateAsync(new CreateBodyMeasurementRequest
        {
            WeightInKilograms = 70m,
        });

        var impact = await service.GetDeletionImpactAsync(created.MeasurementId);
        await service.DeleteAsync(created.MeasurementId, new DeleteBodyMeasurementRequest
        {
            Version = impact.Version,
            ImpactToken = impact.ImpactToken,
        });

        Assert.Multiple(() =>
        {
            Assert.That(impact.AffectedSnapshotCount, Is.Zero);
            Assert.That(impact.AffectsCurrentTarget, Is.False);
            Assert.That(dbContext.BodyMeasurements, Is.Empty);
        });
    }

    [Test]
    public async Task DeleteAsync_WithInvalidToken_ThrowsConflictAndKeepsMeasurement()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, new TestImpactTokenService());
        var created = await service.CreateAsync(new CreateBodyMeasurementRequest
        {
            WeightInKilograms = 70m,
        });

        Assert.ThrowsAsync<BodyMeasurementConflictException>(async () =>
            await service.DeleteAsync(created.MeasurementId, new DeleteBodyMeasurementRequest
            {
                Version = created.Version,
                ImpactToken = "invalid",
            }));

        Assert.That(await dbContext.BodyMeasurements.CountAsync(), Is.EqualTo(1));
    }

    private static BodyMeasurementService CreateService(
        ApplicationDbContext dbContext,
        IBodyMeasurementImpactTokenService? tokenService = null) => new(
        dbContext,
        new TestCurrentUserService(CurrentUserId),
        tokenService ?? new TestImpactTokenService(),
        new FixedTimeProvider(FixedUtcNow));

    private static ApplicationDbContext CreateDbContext()
    {
        var currentUser = new TestCurrentUserService(CurrentUserId);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"BodyMeasurementServiceTests-{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options, currentUser);
    }

    private static BodyMeasurement CreateMeasurement(
        long measurementId,
        long userId,
        DateTimeOffset? measuredAt = null) => new()
        {
            MeasurementId = measurementId,
            UserId = userId,
            WeightInKilograms = 70m,
            MeasuredAt = measuredAt ?? FixedUtcNow,
            Version = Guid.NewGuid(),
        };

    private static BodyProfile CreateProfile() => new()
    {
        UserId = CurrentUserId,
        BirthDate = new DateOnly(1990, 1, 1),
        BiologicalSexCode = "MALE",
        HeightInCentimeters = 175m,
        FitnessGoalCode = "MAINTAIN",
        ActivityLevelCode = "MODERATE",
        TimeZone = "Asia/Taipei",
        Version = Guid.NewGuid(),
    };

    private sealed class TestCurrentUserService(long userId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public long? UserId => userId;
        public string? UserName => "body-measurement-test";
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class TestImpactTokenService : IBodyMeasurementImpactTokenService
    {
        public BodyMeasurementImpactToken Create(
            long userId,
            long measurementId,
            Guid version,
            int affectedSnapshotCount,
            bool affectsCurrentTarget) => new(
            $"valid:{userId}:{measurementId}:{version}",
            FixedUtcNow.AddMinutes(10));

        public bool IsValid(
            string token,
            long userId,
            long measurementId,
            Guid version,
            int affectedSnapshotCount,
            bool affectsCurrentTarget) =>
            token == $"valid:{userId}:{measurementId}:{version}"
            && affectedSnapshotCount == 0
            && !affectsCurrentTarget;
    }
}
