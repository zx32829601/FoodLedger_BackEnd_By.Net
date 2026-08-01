using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Tests.Data;

public class NutrientDisplayOrderModelTests
{
    [Test]
    public void Nutrient_WhenModelIsBuilt_HasRequiredDisplayOrderWithDefault1000()
    {
        using var dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"NutrientDisplayOrder-{Guid.NewGuid()}")
                .Options);

        var property = dbContext.Model.FindEntityType(typeof(Nutrient))!
            .FindProperty(nameof(Nutrient.DisplayOrder));

        Assert.Multiple(() =>
        {
            Assert.That(property, Is.Not.Null);
            Assert.That(property!.IsNullable, Is.False);
            Assert.That(property.GetDefaultValue(), Is.EqualTo(1000));
        });
    }
}
