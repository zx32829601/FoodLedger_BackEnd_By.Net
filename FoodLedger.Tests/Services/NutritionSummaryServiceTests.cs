using FoodLedger.Data.Entities;
using FoodLedger.Services;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Tests.Services;

/// <summary>
/// 驗證每日營養摘要以克數和每 100 克資料進行換算。
/// </summary>
[Category("Nutrition")]
[Category("Unit")]
public class NutritionSummaryServiceTests
{
    /// <summary>
    /// 驗證每日摘要會依食用克數換算每 100 克營養資料。
    /// </summary>
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
        var response = await service.GetDailyAsync(
            new DateOnly(2026, 7, 27),
            "Etc/UTC",
            "zh-TW");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Totals.Single().Code, Is.EqualTo("Protein"));
            Assert.That(response.Totals.Single().Amount, Is.EqualTo(30m));
            Assert.That(response.Totals.Single().UnitCode, Is.EqualTo("g"));
        });
    }

    /// <summary>
    /// 驗證每日摘要以指定時區切分本地日期，只統計目前使用者並依餐別拆分。
    /// </summary>
    [Test]
    public async Task GetDailyAsync_WhenTimeZoneIsAsiaTaipei_ReturnsLocalDayMealBreakdown()
    {
        // Arrange
        await using var dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"NutritionSummary-{Guid.NewGuid()}")
                .Options);
        var food = new SimpleFood { FoodCode = "LOCAL_DAY_FOOD" };
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
            Amount = 10m,
        });
        dbContext.DailyRecords.AddRange(
            CreateRecord(42, food.FoodId, 100m, "Breakfast", 2026, 7, 27, 16, 0),
            CreateRecord(42, food.FoodId, 200m, "Lunch", 2026, 7, 28, 15, 59),
            CreateRecord(42, food.FoodId, 400m, "Dinner", 2026, 7, 28, 16, 0),
            CreateRecord(2, food.FoodId, 800m, "Snack", 2026, 7, 28, 8, 0));
        await dbContext.SaveChangesAsync();
        var service = new NutritionSummaryService(
            dbContext,
            new TestCurrentUserService());

        // Act
        var response = await service.GetDailyAsync(
            new DateOnly(2026, 7, 28),
            "Asia/Taipei",
            "zh-TW");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Totals.Single().Amount, Is.EqualTo(30m));
            Assert.That(response.Totals.Single().DisplayName, Is.EqualTo("蛋白質"));
            Assert.That(
                response.MealTypes.Select(item => (item.MealTypeCode, item.Totals.Single().Amount)),
                Is.EqualTo(new[] { ("Breakfast", 10m), ("Lunch", 20m) }));
        });
    }

    /// <summary>
    /// 驗證週摘要以焦點日期所在週的週一至週日彙總，並固定回傳七天資料。
    /// </summary>
    [Test]
    public async Task GetWeeklyAsync_WhenFocusDateIsMidweek_ReturnsMondayToSundayBreakdown()
    {
        // Arrange
        await using var dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"NutritionSummary-{Guid.NewGuid()}")
                .Options);
        var food = new SimpleFood { FoodCode = "WEEKLY_FOOD" };
        var nutrient = new Nutrient
        {
            NutrientCode = "Calories",
            UnitCode = NutrientUnitCodes.Kilocalorie,
            Translations =
            [
                new NutrientTranslation
                {
                    LangCode = "en-US",
                    NutrientName = "Calories",
                },
            ],
        };
        dbContext.AddRange(food, nutrient);
        await dbContext.SaveChangesAsync();
        dbContext.FoodNutrients.Add(new FoodNutrient
        {
            FoodId = food.FoodId,
            NutrientId = nutrient.NutrientId,
            Amount = 10m,
        });
        dbContext.DailyRecords.AddRange(
            CreateRecord(42, food.FoodId, 100m, "Breakfast", 2026, 7, 26, 16, 0),
            CreateRecord(42, food.FoodId, 200m, "Dinner", 2026, 8, 2, 15, 59),
            CreateRecord(42, food.FoodId, 400m, "Snack", 2026, 8, 2, 16, 0));
        await dbContext.SaveChangesAsync();
        var service = new NutritionSummaryService(
            dbContext,
            new TestCurrentUserService());

        // Act
        var response = await service.GetWeeklyAsync(
            new DateOnly(2026, 7, 29),
            "Asia/Taipei",
            "zh-TW");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StartDate, Is.EqualTo(new DateOnly(2026, 7, 27)));
            Assert.That(response.EndDate, Is.EqualTo(new DateOnly(2026, 8, 2)));
            Assert.That(response.Totals.Single().Amount, Is.EqualTo(30m));
            Assert.That(response.Totals.Single().DisplayName, Is.EqualTo("Calories"));
            Assert.That(response.Totals.Single().LangCode, Is.EqualTo("en-US"));
            Assert.That(response.Days, Has.Count.EqualTo(7));
            Assert.That(response.Days[0].Totals.Single().Amount, Is.EqualTo(10m));
            Assert.That(response.Days[1].Totals, Is.Empty);
            Assert.That(response.Days[6].Totals.Single().Amount, Is.EqualTo(20m));
        });
    }

    /// <summary>
    /// 驗證具有日光節約時間的地區會依當日本地日界查詢，而不是固定二十四小時。
    /// </summary>
    [Test]
    public async Task GetDailyAsync_WhenDaylightSavingTimeStarts_UsesLocalCalendarBoundaries()
    {
        // Arrange
        await using var dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"NutritionSummary-{Guid.NewGuid()}")
                .Options);
        var food = new SimpleFood { FoodCode = "DST_FOOD" };
        var nutrient = new Nutrient
        {
            NutrientCode = "Calories",
            UnitCode = NutrientUnitCodes.Kilocalorie,
        };
        dbContext.AddRange(food, nutrient);
        await dbContext.SaveChangesAsync();
        dbContext.FoodNutrients.Add(new FoodNutrient
        {
            FoodId = food.FoodId,
            NutrientId = nutrient.NutrientId,
            Amount = 10m,
        });
        dbContext.DailyRecords.AddRange(
            CreateRecord(42, food.FoodId, 800m, "Snack", 2026, 3, 8, 4, 59),
            CreateRecord(42, food.FoodId, 100m, "Breakfast", 2026, 3, 8, 5, 0),
            CreateRecord(42, food.FoodId, 200m, "Dinner", 2026, 3, 9, 3, 59),
            CreateRecord(42, food.FoodId, 400m, "Snack", 2026, 3, 9, 4, 0));
        await dbContext.SaveChangesAsync();
        var service = new NutritionSummaryService(
            dbContext,
            new TestCurrentUserService());

        // Act
        var response = await service.GetDailyAsync(
            new DateOnly(2026, 3, 8),
            "America/New_York",
            "zh-TW");

        // Assert
        Assert.That(response.Totals.Single().Amount, Is.EqualTo(30m));
    }

    private static DailyRecord CreateRecord(
        long userId,
        long foodId,
        decimal quantity,
        string mealTypeCode,
        int year,
        int month,
        int day,
        int hour,
        int minute)
    {
        return new DailyRecord
        {
            UserId = userId,
            FoodId = foodId,
            Quantity = quantity,
            ConsumedAt = new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero),
            MealTypeCode = mealTypeCode,
        };
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public long? UserId => 42;
        public string? UserName => "tester";
    }
}
