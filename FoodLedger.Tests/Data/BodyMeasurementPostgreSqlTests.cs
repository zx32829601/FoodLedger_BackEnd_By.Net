using FoodLedger.Data.Entities;
using FoodLedger.DTOs.BodyMeasurements;
using FoodLedger.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FoodLedger.Tests.Data;

/// <summary>以真實 PostgreSQL 驗證身體測量的 schema、並行與交易刪除行為。</summary>
[NonParallelizable]
[Category("BodyMeasurements")]
[Category("Integration")]
public sealed class BodyMeasurementPostgreSqlTests
{
    private const long CurrentUserId = 42;
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);

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
    public async Task Schema_HasExpectedPrecisionIndexConstraintsAndUserCascade()
    {
        await using var dbContext = CreateDbContext(CurrentUserId);
        await AddUserAsync(dbContext, CurrentUserId);
        var service = CreateService(dbContext);
        var created = await service.CreateAsync(CreateRequest());

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var precisionCommand = new NpgsqlCommand(
            """
            SELECT string_agg(
                column_name || ':' || numeric_precision::text || ':' || numeric_scale::text,
                ',' ORDER BY column_name)
            FROM information_schema.columns
            WHERE table_name = 'body_measurement'
              AND column_name IN (
                  'weight_in_kilograms',
                  'body_fat_percentage',
                  'muscle_mass_in_kilograms');
            """,
            connection);
        var precision = await precisionCommand.ExecuteScalarAsync();
        await using var indexCommand = new NpgsqlCommand(
            """
            SELECT indexdef
            FROM pg_indexes
            WHERE tablename = 'body_measurement'
              AND indexname = 'ix_body_measurement_user_history';
            """,
            connection);
        var indexDefinition = (string?)await indexCommand.ExecuteScalarAsync();
        await using var invalidWeightCommand = new NpgsqlCommand(
            """
            INSERT INTO body_measurement (
                user_id,
                weight_in_kilograms,
                measured_at,
                version)
            VALUES (42, 19.99, CURRENT_TIMESTAMP, gen_random_uuid());
            """,
            connection);
        var constraintException = Assert.ThrowsAsync<PostgresException>(
            invalidWeightCommand.ExecuteNonQueryAsync);
        await using var deleteUserCommand = new NpgsqlCommand(
            "DELETE FROM application_user WHERE user_id = 42;",
            connection);
        await deleteUserCommand.ExecuteNonQueryAsync();
        await using var measurementCountCommand = new NpgsqlCommand(
            "SELECT COUNT(*) FROM body_measurement WHERE measurement_id = @measurementId;",
            connection);
        measurementCountCommand.Parameters.AddWithValue("measurementId", created.MeasurementId);
        var measurementCount = await measurementCountCommand.ExecuteScalarAsync();

        Assert.Multiple(() =>
        {
            Assert.That(precision, Is.EqualTo(
                "body_fat_percentage:4:2,muscle_mass_in_kilograms:5:2,"
                + "weight_in_kilograms:5:2"));
            Assert.That(indexDefinition, Does.Contain(
                "user_id, measured_at DESC, created_at DESC, measurement_id DESC"));
            Assert.That(constraintException?.SqlState,
                Is.EqualTo(PostgresErrorCodes.CheckViolation));
            Assert.That(measurementCount, Is.EqualTo(0L));
        });
    }

    [Test]
    public async Task VersionToken_WhenTwoContextsUpdateSameMeasurement_RejectsStaleWrite()
    {
        long measurementId;
        await using (var seedContext = CreateDbContext(CurrentUserId))
        {
            await AddUserAsync(seedContext, CurrentUserId);
            measurementId = (await CreateService(seedContext).CreateAsync(CreateRequest()))
                .MeasurementId;
        }

        await using var firstContext = CreateDbContext(CurrentUserId);
        await using var secondContext = CreateDbContext(CurrentUserId);
        var first = await firstContext.BodyMeasurements.SingleAsync(item =>
            item.MeasurementId == measurementId);
        var second = await secondContext.BodyMeasurements.SingleAsync(item =>
            item.MeasurementId == measurementId);
        first.WeightInKilograms = 71m;
        first.Version = Guid.NewGuid();
        second.WeightInKilograms = 72m;
        second.Version = Guid.NewGuid();

        await firstContext.SaveChangesAsync();

        Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            async () => await secondContext.SaveChangesAsync());
    }

    [Test]
    public async Task DeleteAsync_WithMatchingImpactToken_CommitsRelationalDeletion()
    {
        await using var dbContext = CreateDbContext(CurrentUserId);
        await AddUserAsync(dbContext, CurrentUserId);
        var service = CreateService(dbContext);
        var created = await service.CreateAsync(CreateRequest());
        var impact = await service.GetDeletionImpactAsync(created.MeasurementId);

        await service.DeleteAsync(created.MeasurementId, new DeleteBodyMeasurementRequest
        {
            Version = impact.Version,
            ImpactToken = impact.ImpactToken,
        });

        await using var verificationContext = CreateDbContext(CurrentUserId);
        Assert.That(await verificationContext.BodyMeasurements.CountAsync(), Is.Zero);
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

    private static BodyMeasurementService CreateService(ApplicationDbContext dbContext) =>
        new(
            dbContext,
            new TestCurrentUserService(CurrentUserId),
            new TestImpactTokenService(),
            new FixedTimeProvider(FixedUtcNow));

    private static CreateBodyMeasurementRequest CreateRequest() => new()
    {
        WeightInKilograms = 70.25m,
        BodyFatPercentage = 18.5m,
        MuscleMassInKilograms = 30.75m,
    };

    private static async Task AddUserAsync(
        ApplicationDbContext dbContext,
        long userId)
    {
        dbContext.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = $"measurement-user-{userId}",
            NormalizedUserName = $"MEASUREMENT-USER-{userId}",
            Email = $"measurement-{userId}@example.com",
            NormalizedEmail = $"MEASUREMENT-{userId}@EXAMPLE.COM",
            DisplayName = $"Measurement User {userId}",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        });
        await dbContext.SaveChangesAsync();
    }

    private sealed class TestCurrentUserService(long userId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public long? UserId => userId;
        public string? UserName => $"measurement-user-{userId}";
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
