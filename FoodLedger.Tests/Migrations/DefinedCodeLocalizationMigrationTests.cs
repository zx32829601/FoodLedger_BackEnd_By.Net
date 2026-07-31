using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FoodLedger.Tests.Migrations;

/// <summary>
/// 以真實 PostgreSQL 驗證 DefinedCode 多語系 migration 的資料保留與刪除限制。
/// </summary>
[NonParallelizable]
public sealed class DefinedCodeLocalizationMigrationTests
{
    private const string PreviousMigration = "20260727104018_AddNutrientUnitCode";
    private const string LocalizationMigration = "20260731053950_AddDefinedCodeLocalization";

    private PostgreSqlContainer _postgreSql = default!;

    /// <summary>
    /// 啟動每次測試專用的 PostgreSQL container。
    /// </summary>
    [SetUp]
    public async Task SetUp()
    {
        _postgreSql = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await _postgreSql.StartAsync();
    }

    /// <summary>
    /// 回收本次測試建立的 PostgreSQL container。
    /// </summary>
    [TearDown]
    public async Task TearDown()
    {
        await _postgreSql.DisposeAsync();
    }

    /// <summary>
    /// 驗證升級會保留舊顯示名稱、禁止實體刪除，且 rollback 僅使用中英文或原始代碼。
    /// </summary>
    [Test]
    [Category("Integration")]
    public async Task Migrate_WhenDefinedCodesExist_PreservesDataAndDeleteInvariant()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var migrator = dbContext.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);

        await ExecuteNonQueryAsync(
            """
            INSERT INTO defined_code
                (code_type, code, display_name, sort_order, is_active, created_at, created_by, modified_at)
            VALUES
                ('CUSTOM_TYPE', 'CUSTOM_CODE', '自訂名稱', 1, TRUE, CURRENT_TIMESTAMP, 'Test', CURRENT_TIMESTAMP);
            """);

        // Act
        await migrator.MigrateAsync(LocalizationMigration);

        // Assert
        Assert.That(
            await ExecuteScalarAsync<string>(
                """
                SELECT display_name
                FROM defined_code_translation
                WHERE code_type = 'CUSTOM_TYPE'
                  AND code = 'CUSTOM_CODE'
                  AND lang_code = 'zh-TW';
                """),
            Is.EqualTo("自訂名稱"));

        var deleteException = Assert.ThrowsAsync<PostgresException>(() =>
            ExecuteNonQueryAsync(
                """
                DELETE FROM defined_code
                WHERE code_type = 'CUSTOM_TYPE' AND code = 'CUSTOM_CODE';
                """));
        Assert.That(deleteException?.SqlState, Is.EqualTo(PostgresErrorCodes.ForeignKeyViolation));

        await ExecuteNonQueryAsync(
            """
            INSERT INTO defined_code
                (code_type, code, sort_order, is_active, created_at, created_by, modified_at)
            VALUES
                ('CUSTOM_TYPE', 'FRENCH_ONLY', 2, TRUE, CURRENT_TIMESTAMP, 'Test', CURRENT_TIMESTAMP);

            INSERT INTO defined_code_translation
                (code_type, code, lang_code, display_name, note, created_at, created_by, modified_at)
            VALUES
                ('CUSTOM_TYPE', 'FRENCH_ONLY', 'fr-FR', 'Français', NULL, CURRENT_TIMESTAMP, 'Test', CURRENT_TIMESTAMP);
            """);

        await migrator.MigrateAsync(PreviousMigration);

        var restoredCustomDisplayName = await ExecuteScalarAsync<string>(
            """
            SELECT display_name
            FROM defined_code
            WHERE code_type = 'CUSTOM_TYPE' AND code = 'CUSTOM_CODE';
            """);
        var restoredFrenchOnlyDisplayName = await ExecuteScalarAsync<string>(
            """
            SELECT display_name
            FROM defined_code
            WHERE code_type = 'CUSTOM_TYPE' AND code = 'FRENCH_ONLY';
            """);

        Assert.Multiple(() =>
        {
            Assert.That(restoredCustomDisplayName, Is.EqualTo("自訂名稱"));
            Assert.That(restoredFrenchOnlyDisplayName, Is.EqualTo("FRENCH_ONLY"));
        });
    }

    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgreSql.GetConnectionString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private async Task ExecuteNonQueryAsync(string commandText)
    {
        await using var connection = new NpgsqlConnection(_postgreSql.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(commandText, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<T?> ExecuteScalarAsync<T>(string commandText)
    {
        await using var connection = new NpgsqlConnection(_postgreSql.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(commandText, connection);
        return (T?)await command.ExecuteScalarAsync();
    }
}
