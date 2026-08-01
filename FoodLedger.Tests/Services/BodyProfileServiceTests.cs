using FoodLedger.Data.Entities;
using FoodLedger.DTOs.BodyProfiles;
using FoodLedger.Services;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Tests.Services;

/// <summary>
/// 驗證身體資料的所有權、驗證與樂觀並行控制。
/// </summary>
public sealed class BodyProfileServiceTests
{
    private const long CurrentUserId = 42;
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 8, 1, 4, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task UpsertAsync_WhenProfileDoesNotExist_CreatesProfileForCurrentUser()
    {
        await using var dbContext = CreateDbContext();
        SeedActiveCodes(dbContext);
        var service = CreateService(dbContext);

        var response = await service.UpsertAsync(CreateRequest());

        var entity = await dbContext.BodyProfiles.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(entity.UserId, Is.EqualTo(CurrentUserId));
            Assert.That(entity.BirthDate, Is.EqualTo(new DateOnly(1990, 5, 20)));
            Assert.That(response.Version, Is.Not.EqualTo(Guid.Empty));
        });
    }

    [Test]
    public async Task GetAsync_WhenProfileDoesNotExist_ThrowsKeyNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await service.GetAsync());
    }

    [Test]
    public async Task UpsertAsync_WhenVersionMatches_UpdatesAndRotatesVersion()
    {
        await using var dbContext = CreateDbContext();
        SeedActiveCodes(dbContext);
        var service = CreateService(dbContext);
        var created = await service.UpsertAsync(CreateRequest());
        var request = CreateRequest(created.Version);
        request.HeightInCentimeters = 180m;

        var updated = await service.UpsertAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(updated.HeightInCentimeters, Is.EqualTo(180m));
            Assert.That(updated.Version, Is.Not.EqualTo(created.Version));
        });
    }

    [Test]
    public async Task UpsertAsync_WhenVersionIsStale_ThrowsConflictAndKeepsProfile()
    {
        await using var dbContext = CreateDbContext();
        SeedActiveCodes(dbContext);
        var service = CreateService(dbContext);
        var created = await service.UpsertAsync(CreateRequest());
        var validUpdate = CreateRequest(created.Version);
        validUpdate.HeightInCentimeters = 180m;
        await service.UpsertAsync(validUpdate);
        var staleUpdate = CreateRequest(created.Version);
        staleUpdate.HeightInCentimeters = 190m;

        Assert.ThrowsAsync<BodyProfileConflictException>(
            async () => await service.UpsertAsync(staleUpdate));
        Assert.That((await dbContext.BodyProfiles.SingleAsync()).HeightInCentimeters,
            Is.EqualTo(180m));
    }

    [TestCase("2008-08-02")]
    [TestCase("1905-08-01")]
    public async Task UpsertAsync_WhenAgeIsOutsideAdultRange_RejectsBirthDate(string birthDate)
    {
        await using var dbContext = CreateDbContext();
        SeedActiveCodes(dbContext);
        var service = CreateService(dbContext);
        var request = CreateRequest();
        request.BirthDate = DateOnly.Parse(birthDate);

        var exception = Assert.ThrowsAsync<BodyProfileValidationException>(
            async () => await service.UpsertAsync(request));

        Assert.Multiple(() =>
        {
            Assert.That(exception?.FieldName, Is.EqualTo(nameof(request.BirthDate)));
            Assert.That(exception?.ErrorCode, Is.EqualTo("BodyProfile.AgeOutOfRange"));
        });
    }

    [TestCase("2008-08-01")]
    [TestCase("1906-08-01")]
    public async Task UpsertAsync_WhenAgeIsOnInclusiveBoundary_AcceptsBirthDate(
        string birthDate)
    {
        await using var dbContext = CreateDbContext();
        SeedActiveCodes(dbContext);
        var service = CreateService(dbContext);
        var request = CreateRequest();
        request.BirthDate = DateOnly.Parse(birthDate);

        var result = await service.UpsertAsync(request);

        Assert.That(result.BirthDate, Is.EqualTo(request.BirthDate));
    }

    [TestCase(99.99)]
    [TestCase(250.01)]
    public async Task UpsertAsync_WhenHeightIsOutsideRange_RejectsHeight(decimal height)
    {
        await using var dbContext = CreateDbContext();
        SeedActiveCodes(dbContext);
        var service = CreateService(dbContext);
        var request = CreateRequest();
        request.HeightInCentimeters = height;

        var exception = Assert.ThrowsAsync<BodyProfileValidationException>(
            async () => await service.UpsertAsync(request));

        Assert.That(exception?.ErrorCode, Is.EqualTo("BodyProfile.HeightOutOfRange"));
    }

    [Test]
    public async Task UpsertAsync_WhenTimeZoneIsNotIana_RejectsTimeZone()
    {
        await using var dbContext = CreateDbContext();
        SeedActiveCodes(dbContext);
        var service = CreateService(dbContext);
        var request = CreateRequest();
        request.TimeZone = "Taipei Standard Time";

        var exception = Assert.ThrowsAsync<BodyProfileValidationException>(
            async () => await service.UpsertAsync(request));

        Assert.That(exception?.ErrorCode, Is.EqualTo("BodyProfile.InvalidTimeZone"));
    }

    [TestCase("FitnessGoalCode", "UNKNOWN")]
    [TestCase("ActivityLevelCode", "UNKNOWN")]
    public async Task UpsertAsync_WhenDefinedCodeIsInactiveOrMissing_RejectsCode(
        string field,
        string code)
    {
        await using var dbContext = CreateDbContext();
        SeedActiveCodes(dbContext);
        var service = CreateService(dbContext);
        var request = CreateRequest();
        if (field == nameof(request.FitnessGoalCode))
        {
            request.FitnessGoalCode = code;
        }
        else
        {
            request.ActivityLevelCode = code;
        }

        var exception = Assert.ThrowsAsync<BodyProfileValidationException>(
            async () => await service.UpsertAsync(request));

        Assert.That(exception?.FieldName, Is.EqualTo(field));
    }

    private static BodyProfileService CreateService(ApplicationDbContext dbContext) =>
        new(
            dbContext,
            new TestCurrentUserService(),
            new FixedTimeProvider(FixedUtcNow));

    private static UpsertBodyProfileRequest CreateRequest(Guid? version = null) => new()
    {
        BirthDate = new DateOnly(1990, 5, 20),
        BiologicalSexCode = "MALE",
        HeightInCentimeters = 175.5m,
        FitnessGoalCode = "MAINTAIN",
        ActivityLevelCode = "MODERATE",
        TimeZone = "Asia/Taipei",
        Version = version,
    };

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"BodyProfileServiceTests-{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void SeedActiveCodes(ApplicationDbContext dbContext)
    {
        dbContext.DefinedCodes.AddRange(
            new DefinedCode
            {
                CodeType = DefinedCodeTypes.FitnessGoal,
                Code = "MAINTAIN",
                SortOrder = 1,
                IsActive = true,
            },
            new DefinedCode
            {
                CodeType = DefinedCodeTypes.ActivityLevel,
                Code = "MODERATE",
                SortOrder = 1,
                IsActive = true,
            });
        dbContext.SaveChanges();
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public long? UserId => CurrentUserId;
        public string? UserName => "body-profile-test";
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
