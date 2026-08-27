using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FoodLedger.Tests.Data;

/// <summary>驗證身體測量的 EF schema 契約。</summary>
[Category("BodyMeasurements")]
[Category("Unit")]
public sealed class BodyMeasurementModelTests
{
    [Test]
    public void Model_HasExpectedPrecisionConcurrencyIndexAndCascadeDelete()
    {
        using var dbContext = CreateDbContext();
        var model = dbContext.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(BodyMeasurement))!;
        var weight = entity.FindProperty(nameof(BodyMeasurement.WeightInKilograms))!;
        var bodyFat = entity.FindProperty(nameof(BodyMeasurement.BodyFatPercentage))!;
        var muscle = entity.FindProperty(nameof(BodyMeasurement.MuscleMassInKilograms))!;
        var version = entity.FindProperty(nameof(BodyMeasurement.Version))!;
        var index = entity.GetIndexes().Single(item =>
            item.GetDatabaseName() == "ix_body_measurement_user_history");
        var foreignKey = entity.GetForeignKeys().Single(item =>
            item.Properties.Single().Name == nameof(BodyMeasurement.UserId));

        Assert.Multiple(() =>
        {
            Assert.That((weight.GetPrecision(), weight.GetScale()), Is.EqualTo((5, 2)));
            Assert.That((bodyFat.GetPrecision(), bodyFat.GetScale()), Is.EqualTo((4, 2)));
            Assert.That((muscle.GetPrecision(), muscle.GetScale()), Is.EqualTo((5, 2)));
            Assert.That(version.IsConcurrencyToken, Is.True);
            Assert.That(index.Properties.Select(property => property.Name), Is.EqualTo([
                nameof(BodyMeasurement.UserId),
                nameof(BodyMeasurement.MeasuredAt),
                nameof(BodyMeasurement.CreatedAt),
                nameof(BodyMeasurement.MeasurementId),
            ]));
            Assert.That(index.IsDescending, Is.EqualTo([false, true, true, true]));
            Assert.That(foreignKey.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade));
        });
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"BodyMeasurementModelTests-{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
