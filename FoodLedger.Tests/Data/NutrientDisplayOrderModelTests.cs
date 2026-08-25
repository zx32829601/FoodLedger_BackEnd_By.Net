using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Tests.Data;

/// <summary>
/// 驗證營養素顯示順序的 EF Core 預設值與儲存行為。
/// </summary>
[Category("Nutrition")]
[Category("Unit")]
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
