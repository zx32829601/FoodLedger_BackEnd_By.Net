using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace FoodLedger.Tests.Data;

/// <summary>
/// Verifies that the runtime relational model stays synchronized with the latest migration.
/// </summary>
[Category("Database")]
[Category("Unit")]
public sealed class MigrationModelConsistencyTests
{
    [Test]
    public void ApplicationDbContext_WhenComparedWithLatestMigration_HasNoPendingChanges()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_check;Username=model_check;Password=model_check")
            .Options;

        using var dbContext = new ApplicationDbContext(options);

        var migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();
        var modelDiffer = dbContext.GetService<IMigrationsModelDiffer>();
        var snapshotModel = migrationsAssembly.ModelSnapshot?.Model
            ?? throw new InvalidOperationException("The migration snapshot is missing.");
        var initializedSnapshot = dbContext.GetService<IModelRuntimeInitializer>()
            .Initialize(snapshotModel, designTime: true);
        var designTimeModel = dbContext.GetService<IDesignTimeModel>().Model;
        var operations = modelDiffer.GetDifferences(
            initializedSnapshot.GetRelationalModel(),
            designTimeModel.GetRelationalModel());
        var operationDescriptions = operations.Select(operation => operation switch
        {
            AlterColumnOperation alter =>
                $"{alter.Table}.{alter.Name}: "
                + $"{alter.OldColumn.ColumnType}/{alter.OldColumn.IsNullable}/"
                + $"{alter.OldColumn.DefaultValue}/{alter.OldColumn.DefaultValueSql} -> "
                + $"{alter.ColumnType}/{alter.IsNullable}/"
                + $"{alter.DefaultValue}/{alter.DefaultValueSql}",
            _ => operation.GetType().Name,
        });

        Assert.That(
            dbContext.Database.HasPendingModelChanges(),
            Is.False,
            $"Pending operations: {string.Join("; ", operationDescriptions)}");
    }
}
