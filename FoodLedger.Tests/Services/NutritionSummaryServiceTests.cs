using FoodLedger.Data.Entities;
using FoodLedger.Services;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Tests.Services;

/// <summary>
/// 驗證每日營養摘要以克數和每 100 克資料進行換算。
/// </summary>
public class NutritionSummaryServiceTests
{
    [Test]
    public async Task GetDailyAsync_WhenRecordExists_ReturnsScaledNutrientAsync()
    {
        // Arrange
        await using var dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"NutritionSummary-{Guid.NewGuid()}")
                .Options);
        var food = new SimpleFood { FoodCode = "CHICKEN" };
        var nutrient = new Nutrient
        {
            NutrientCode = "Protein",
            UnitCode = NutrientUnitCodes.Gram,
            Translations =
            [
                new NutrientTranslation
                {
                    LangCode = "zh-TW",
                    NutrientName = "蛋白質",
                },
            ],
        };
        dbContext.AddRange(food, nutrient);
        await dbContext.SaveChangesAsync();
        dbContext.FoodNutrients.Add(new FoodNutrient
        {
            FoodId = food.FoodId,
            NutrientId = nutrient.NutrientId,
            Amount = 20m,
        });
        dbContext.DailyRecords.Add(new DailyRecord
        {
            UserId = 42,
            FoodId = food.FoodId,
            Quantity = 150m,
            ConsumedAt = new DateTimeOffset(2026, 7, 27, 12, 0, 0, TimeSpan.Zero),
            MealTypeCode = MealTypeCodes.Snack,
        });
        await dbContext.SaveChangesAsync();
        var service = new NutritionSummaryService(
            dbContext,
            new TestCurrentUserService());

        // Act
        var response = await service.GetDailyAsync(new DateOnly(2026, 7, 27));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Totals.Single().Code, Is.EqualTo("Protein"));
            Assert.That(response.Totals.Single().Amount, Is.EqualTo(30m));
            Assert.That(response.Totals.Single().UnitCode, Is.EqualTo("g"));
        });
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public long? UserId => 42;
        public string? UserName => "tester";
    }
}
