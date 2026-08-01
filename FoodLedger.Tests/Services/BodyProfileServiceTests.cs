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

        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await service.GetAsync("zh-TW"));
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
    [TestCase("1905-08-02")]
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
    public async Task UpsertAsync_WhenHeightHasMoreThanTwoDecimals_RejectsHeight()
    {
        await using var dbContext = CreateDbContext();
        SeedActiveCodes(dbContext);
        var service = CreateService(dbContext);
        var request = CreateRequest();
        request.HeightInCentimeters = 175.555m;

        var exception = Assert.ThrowsAsync<BodyProfileValidationException>(
            async () => await service.UpsertAsync(request));

        Assert.That(exception?.ErrorCode,
            Is.EqualTo("BodyProfile.HeightPrecisionExceeded"));
    }

    [TestCase(100)]
    [TestCase(250)]
    public async Task UpsertAsync_WhenHeightIsOnInclusiveBoundary_AcceptsHeight(
        decimal height)
    {
        await using var dbContext = CreateDbContext();
        SeedActiveCodes(dbContext);
        var service = CreateService(dbContext);
        var request = CreateRequest();
        request.HeightInCentimeters = height;

        var result = await service.UpsertAsync(request);

        Assert.That(result.HeightInCentimeters, Is.EqualTo(height));
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

    [TestCase("FitnessGoalCode", "INACTIVE")]
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

    [Test]
    public async Task GetAsync_WhenStoredCodeIsInactive_ReturnsLocalizedHistoricalLabel()
    {
        await using var dbContext = CreateDbContext();
        SeedActiveCodes(dbContext);
        var service = CreateService(dbContext);
        await service.UpsertAsync(CreateRequest());
        var goal = await dbContext.DefinedCodes.SingleAsync(item =>
            item.CodeType == DefinedCodeTypes.FitnessGoal && item.Code == "MAINTAIN");
        goal.IsActive = false;
        await dbContext.SaveChangesAsync();

        var result = await service.GetAsync("fr-FR");

        Assert.Multiple(() =>
        {
            Assert.That(result.FitnessGoalDisplayName, Is.EqualTo("Maintain weight"));
            Assert.That(result.FitnessGoalLangCode, Is.EqualTo("en-US"));
            Assert.That(result.FitnessGoalNote, Is.EqualTo("Keep current weight."));
        });
    }

    [Test]
    public void UpsertAsync_WhenBirthdayHasNotArrivedInConfiguredTimeZone_RejectsAge()
    {
        using var dbContext = CreateDbContext();
        SeedActiveCodes(dbContext);
        var service = CreateService(
            dbContext,
            now: new DateTimeOffset(2026, 3, 8, 4, 30, 0, TimeSpan.Zero));
        var request = CreateRequest();
        request.BirthDate = new DateOnly(2008, 3, 8);
        request.TimeZone = "America/New_York";

        var exception = Assert.ThrowsAsync<BodyProfileValidationException>(
            async () => await service.UpsertAsync(request));

        Assert.That(exception?.ErrorCode, Is.EqualTo("BodyProfile.AgeOutOfRange"));
    }

    [Test]
    public async Task UpsertAsync_WhenBirthdayArrivesInConfiguredTimeZone_AcceptsAge()
    {
        await using var dbContext = CreateDbContext();
        SeedActiveCodes(dbContext);
        var service = CreateService(
            dbContext,
            now: new DateTimeOffset(2026, 3, 8, 5, 30, 0, TimeSpan.Zero));
        var request = CreateRequest();
        request.BirthDate = new DateOnly(2008, 3, 8);
        request.TimeZone = "America/New_York";

        var result = await service.UpsertAsync(request);

        Assert.That(result.BirthDate, Is.EqualTo(request.BirthDate));
    }

    [Test]
    public async Task UpsertAsync_WhenBirthdayPassedInConfiguredTimeZone_AcceptsAge()
    {
        await using var dbContext = CreateDbContext();
        SeedActiveCodes(dbContext);
        var service = CreateService(
            dbContext,
            now: new DateTimeOffset(2026, 3, 9, 5, 0, 0, TimeSpan.Zero));
        var request = CreateRequest();
        request.BirthDate = new DateOnly(2008, 3, 8);
        request.TimeZone = "America/New_York";

        var result = await service.UpsertAsync(request);

        Assert.That(result.BirthDate, Is.EqualTo(request.BirthDate));
    }

    [Test]
    public void UpsertAsync_WhenBiologicalSexIsUnsupported_RejectsCode()
    {
        using var dbContext = CreateDbContext();
        SeedActiveCodes(dbContext);
        var service = CreateService(dbContext);
        var request = CreateRequest();
        request.BiologicalSexCode = "UNKNOWN";

        var exception = Assert.ThrowsAsync<BodyProfileValidationException>(
            async () => await service.UpsertAsync(request));

        Assert.Multiple(() =>
        {
            Assert.That(exception?.FieldName,
                Is.EqualTo(nameof(request.BiologicalSexCode)));
            Assert.That(exception?.ErrorCode,
                Is.EqualTo("BodyProfile.InvalidBiologicalSex"));
        });
    }

    [Test]
    public async Task GetAsync_WhenTwoUsersHaveProfiles_ReturnsOnlyCurrentUsersProfile()
    {
        await using var dbContext = CreateDbContext();
        SeedActiveCodes(dbContext);
        var firstService = CreateService(dbContext, userId: 42);
        var secondService = CreateService(dbContext, userId: 43);
        await firstService.UpsertAsync(CreateRequest());
        var secondRequest = CreateRequest();
        secondRequest.HeightInCentimeters = 188m;
        await secondService.UpsertAsync(secondRequest);

        var first = await firstService.GetAsync("zh-TW");
        var second = await secondService.GetAsync("zh-TW");

        Assert.Multiple(() =>
        {
            Assert.That(first.HeightInCentimeters, Is.EqualTo(175.5m));
            Assert.That(second.HeightInCentimeters, Is.EqualTo(188m));
        });
    }

    private static BodyProfileService CreateService(
        ApplicationDbContext dbContext,
        long userId = CurrentUserId,
        DateTimeOffset? now = null) =>
        new(
            dbContext,
            new TestCurrentUserService(userId),
            new FixedTimeProvider(now ?? FixedUtcNow));

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
                Translations =
                [
                    new DefinedCodeTranslation
                    {
                        CodeType = DefinedCodeTypes.FitnessGoal,
                        Code = "MAINTAIN",
                        LangCode = "en-US",
                        DisplayName = "Maintain weight",
                        Note = "Keep current weight.",
                    },
                ],
            },
            new DefinedCode
            {
                CodeType = DefinedCodeTypes.ActivityLevel,
                Code = "MODERATE",
                SortOrder = 1,
                IsActive = true,
            },
            new DefinedCode
            {
                CodeType = DefinedCodeTypes.FitnessGoal,
                Code = "INACTIVE",
                SortOrder = 2,
                IsActive = false,
            });
        dbContext.SaveChanges();
    }

    private sealed class TestCurrentUserService(long userId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public long? UserId => userId;
        public string? UserName => "body-profile-test";
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
