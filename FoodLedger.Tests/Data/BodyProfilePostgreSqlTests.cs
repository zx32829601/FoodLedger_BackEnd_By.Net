using FoodLedger.Data.Entities;
using FoodLedger.DTOs.BodyProfiles;
using FoodLedger.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FoodLedger.Tests.Data;

/// <summary>
/// 以真實 PostgreSQL 驗證 Body Profile 的 schema、關聯與並行行為。
/// </summary>
[NonParallelizable]
[Category("BodyProfiles")]
[Category("Integration")]
public sealed class BodyProfilePostgreSqlTests
{
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 8, 1, 4, 0, 0, TimeSpan.Zero);

    private PostgreSqlContainer? _postgreSql;

    [SetUp]
    public async Task SetUp()
    {
        _postgreSql = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await _postgreSql.StartAsync();
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_postgreSql is not null)
        {
            await _postgreSql.DisposeAsync();
        }
    }

    [Test]
    public async Task Schema_EnforcesPrecisionUserCascadeAndDefinedCodeDeleteInvariant()
    {
        await using var dbContext = CreateDbContext();
        await AddUserAsync(dbContext, 42);
        var service = CreateService(dbContext, 42);
        await service.UpsertAsync(CreateRequest());

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var columnCommand = new NpgsqlCommand(
            """
            SELECT numeric_precision::text || ':' || numeric_scale::text
            FROM information_schema.columns
            WHERE table_name = 'body_profile'
              AND column_name = 'height_in_centimeters';
            """,
            connection);
        var precision = await columnCommand.ExecuteScalarAsync();

        await using var deleteCodeCommand = new NpgsqlCommand(
            """
            DELETE FROM defined_code
            WHERE code_type = 'FITNESS_GOAL' AND code = 'MAINTAIN';
            """,
            connection);
        var deleteCodeException = Assert.ThrowsAsync<PostgresException>(
            deleteCodeCommand.ExecuteNonQueryAsync);

        await using var deleteUserCommand = new NpgsqlCommand(
            "DELETE FROM application_user WHERE user_id = 42;",
            connection);
        await deleteUserCommand.ExecuteNonQueryAsync();
        await using var profileCountCommand = new NpgsqlCommand(
            "SELECT COUNT(*) FROM body_profile WHERE user_id = 42;",
            connection);
        var profileCount = await profileCountCommand.ExecuteScalarAsync();

        Assert.Multiple(() =>
        {
            Assert.That(precision, Is.EqualTo("5:2"));
            Assert.That(deleteCodeException?.SqlState,
                Is.EqualTo(PostgresErrorCodes.ForeignKeyViolation));
            Assert.That(profileCount, Is.EqualTo(0L));
        });
    }

    [Test]
    public async Task VersionToken_WhenTwoContextsUpdateSameProfile_RejectsStaleWrite()
    {
        await using (var seedContext = CreateDbContext())
        {
            await AddUserAsync(seedContext, 42);
            await CreateService(seedContext, 42).UpsertAsync(CreateRequest());
        }

        await using var firstContext = CreateDbContext();
        await using var secondContext = CreateDbContext();
        var first = await firstContext.BodyProfiles.SingleAsync(item => item.UserId == 42);
        var second = await secondContext.BodyProfiles.SingleAsync(item => item.UserId == 42);
        first.HeightInCentimeters = 180m;
        first.Version = Guid.NewGuid();
        second.HeightInCentimeters = 190m;
        second.Version = Guid.NewGuid();

        await firstContext.SaveChangesAsync();

        Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            async () => await secondContext.SaveChangesAsync());
    }

    [Test]
    public async Task UpsertAsync_WhenDatabaseErrorIsNotDuplicateProfile_DoesNotMapConflict()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, 999);

        var exception = Assert.ThrowsAsync<DbUpdateException>(
            async () => await service.UpsertAsync(CreateRequest()));

        Assert.That(exception, Is.Not.TypeOf<BodyProfileConflictException>());
    }

    [Test]
    public async Task UpsertAsync_WhenTwoFirstCreatesRace_ReturnsOneConflict()
    {
        await using (var seedContext = CreateDbContext())
        {
            await AddUserAsync(seedContext, 42);
        }

        await using var firstContext = CreateDbContext();
        await using var secondContext = CreateDbContext();
        var results = await Task.WhenAll(
            TryCreateAsync(CreateService(firstContext, 42)),
            TryCreateAsync(CreateService(secondContext, 42)));

        Assert.Multiple(() =>
        {
            Assert.That(results.Count(result => result is null), Is.EqualTo(1));
            Assert.That(results.Count(result => result is BodyProfileConflictException),
                Is.EqualTo(1));
        });
    }

    private string ConnectionString => _postgreSql!.GetConnectionString();

    private ApplicationDbContext CreateDbContext(long? userId = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new ApplicationDbContext(
            options,
            userId.HasValue ? new TestCurrentUserService(userId.Value) : null);
    }

    private static BodyProfileService CreateService(
        ApplicationDbContext dbContext,
        long userId) => new(
            dbContext,
            new TestCurrentUserService(userId),
            new FixedTimeProvider(FixedUtcNow));

    private static async Task AddUserAsync(
        ApplicationDbContext dbContext,
        long userId)
    {
        dbContext.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = $"profile-user-{userId}",
            NormalizedUserName = $"PROFILE-USER-{userId}",
            Email = $"profile-{userId}@example.com",
            NormalizedEmail = $"PROFILE-{userId}@EXAMPLE.COM",
            DisplayName = $"Profile User {userId}",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        });
        await dbContext.SaveChangesAsync();
    }

    private static async Task<Exception?> TryCreateAsync(BodyProfileService service)
    {
        try
        {
            await service.UpsertAsync(CreateRequest());
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static UpsertBodyProfileRequest CreateRequest() => new()
    {
        BirthDate = new DateOnly(1990, 5, 20),
        BiologicalSexCode = "MALE",
        HeightInCentimeters = 175.5m,
        FitnessGoalCode = "MAINTAIN",
        ActivityLevelCode = "MODERATE",
        TimeZone = "Asia/Taipei",
    };

    private sealed class TestCurrentUserService(long userId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public long? UserId => userId;
        public string? UserName => $"profile-user-{userId}";
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
