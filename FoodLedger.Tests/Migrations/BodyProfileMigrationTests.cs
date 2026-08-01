using System.Reflection;
using FoodLedger.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace FoodLedger.Tests.Migrations;

/// <summary>
/// 驗證 Body Profile migration 包含建立完整資料表所需的操作。
/// </summary>
public sealed class BodyProfileMigrationTests
{
    [Test]
    public void Up_WhenMigrationIsBuilt_CreatesBodyProfileWithVersion()
    {
        var operations = BuildUpOperations(new AddBodyProfile());

        var table = operations.OfType<CreateTableOperation>()
            .Single(operation => operation.Name == "body_profile");
        Assert.Multiple(() =>
        {
            Assert.That(table.Columns.Select(column => column.Name),
                Does.Contain("version"));
            Assert.That(table.Columns.Select(column => column.Name),
                Does.Contain("time_zone"));
            Assert.That(table.ForeignKeys.Single().PrincipalTable,
                Is.EqualTo("application_user"));
        });
    }

    private static IReadOnlyList<MigrationOperation> BuildUpOperations(Migration migration)
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var method = migration.GetType().GetMethod(
            "Up",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("找不到 migration Up 方法。");
        method.Invoke(migration, [builder]);
        return builder.Operations;
    }
}
