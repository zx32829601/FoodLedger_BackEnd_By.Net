using FoodLedger.Data.Entities;
using FoodLedger.DTOs.Foods;
using FoodLedger.Services;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Tests.Services;

/// <summary>
/// 驗證食物 aggregate 建立、更新、刪除與參照完整性。
/// </summary>
public class FoodMaintenanceServiceTests
{
    [Test]
    public async Task CreateAsync_WhenRequestIsValid_CreatesAggregateAsync()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.Nutrients.Add(new Nutrient
        {
            NutrientCode = "Protein",
            UnitCode = NutrientUnitCodes.Gram,
        });
        await dbContext.SaveChangesAsync();
        var service = new FoodMaintenanceService(dbContext);

        // Act
        var response = await service.CreateAsync(CreateRequest("CHICKEN", 31m));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.FoodCode, Is.EqualTo("CHICKEN"));
            Assert.That(response.Translations.Single().DisplayName, Is.EqualTo("雞胸肉"));
            Assert.That(response.Nutrients.Single().AmountPer100Grams, Is.EqualTo(31m));
        });
    }

    [Test]
    public async Task UpdateAsync_WhenRequestIsValid_ReplacesTranslationsAndNutrientsAsync()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var nutrient = new Nutrient
        {
            NutrientCode = "Protein",
            UnitCode = NutrientUnitCodes.Gram,
        };
        var food = new SimpleFood
        {
            FoodCode = "OLD",
            Translations =
            [
                new SimpleFoodTranslation { LangCode = "en-US", FoodName = "Old" },
            ],
        };
        dbContext.AddRange(nutrient, food);
        await dbContext.SaveChangesAsync();
        var service = new FoodMaintenanceService(dbContext);

        // Act
        var response = await service.UpdateAsync(food.FoodId, CreateRequest("NEW", 25m));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.FoodCode, Is.EqualTo("NEW"));
            Assert.That(response.Translations.Select(item => item.LangCode), Is.EqualTo(["zh-TW"]));
            Assert.That(response.Nutrients.Single().AmountPer100Grams, Is.EqualTo(25m));
        });
    }

    [Test]
    public async Task CreateAsync_WhenNutrientDoesNotExist_ReturnsValidationFailureAsync()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var service = new FoodMaintenanceService(dbContext);

        // Act
        var exception = Assert.ThrowsAsync<FoodMaintenanceValidationException>(
            async () => await service.CreateAsync(CreateRequest("CHICKEN", 31m)));

        // Assert
        Assert.That(exception!.ErrorCode, Is.EqualTo(FoodMaintenanceErrorCodes.NutrientNotFound));
    }

    [Test]
    public async Task DeleteAsync_WhenFoodIsUsedByRecord_RejectsDeleteAsync()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = 1,
            UserName = "tester",
            DisplayName = "Tester",
        };
        var food = new SimpleFood { FoodCode = "CHICKEN" };
        dbContext.AddRange(user, food);
        await dbContext.SaveChangesAsync();
        dbContext.DailyRecords.Add(new DailyRecord
        {
            UserId = user.Id,
            FoodId = food.FoodId,
            Quantity = 100,
            ConsumedAt = DateTimeOffset.UtcNow,
            MealTypeCode = MealTypeCodes.Snack,
        });
        await dbContext.SaveChangesAsync();
        var service = new FoodMaintenanceService(dbContext);

        // Act / Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.DeleteAsync(food.FoodId));
    }

    private static UpsertFoodRequest CreateRequest(string foodCode, decimal protein)
    {
        return new UpsertFoodRequest
        {
            FoodCode = foodCode,
            Translations =
            [
                new UpsertFoodTranslationRequest
                {
                    LangCode = "zh-TW",
                    DisplayName = "雞胸肉",
                },
            ],
            Nutrients =
            [
                new UpsertFoodNutrientRequest
                {
                    NutrientCode = "Protein",
                    AmountPer100Grams = protein,
                },
            ],
        };
    }

    private static ApplicationDbContext CreateDbContext()
    {
        return new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"FoodMaintenance-{Guid.NewGuid()}")
                .Options);
    }
}
