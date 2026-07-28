using FoodLedger.Data.Entities;
using FoodLedger.Services;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Tests.Services;

/// <summary>
/// 驗證 Daily Record 查詢的本地日界與翻譯行為。
/// </summary>
public partial class DailyRecordServiceTests
{
    /// <summary>
    /// 驗證查詢會使用指定時區切分日期，並套用食物與營養素語系。
    /// </summary>
    [Test]
    public async Task GetDailyRecordsAsync_WhenTimeZoneAndLangCodeAreProvided_UsesLocalDayAndTranslation()
    {
        // Arrange
        await using var dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"DailyRecordLocalization-{Guid.NewGuid()}")
                .Options);
        dbContext.SimpleFoods.Add(new SimpleFood
        {
            FoodId = 1,
            FoodCode = "TEST_FOOD",
            Translations =
            [
                new SimpleFoodTranslation
                {
                    LangCode = "zh-TW",
                    FoodName = "測試食物",
                },
                new SimpleFoodTranslation
                {
                    LangCode = "en-US",
                    FoodName = "Test food",
                },
            ],
        });
        dbContext.Nutrients.Add(new Nutrient
        {
            NutrientId = 1,
            NutrientCode = "Protein",
            UnitCode = "g",
            Translations =
            [
                new NutrientTranslation
                {
                    TranslationId = 1,
                    LangCode = "zh-TW",
                    NutrientName = "蛋白質",
                },
                new NutrientTranslation
                {
                    TranslationId = 2,
                    LangCode = "en-US",
                    NutrientName = "Protein",
                },
            ],
        });
        dbContext.FoodNutrients.Add(new FoodNutrient
        {
            FoodId = 1,
            NutrientId = 1,
            Amount = 20,
        });
        dbContext.DailyRecords.AddRange(
            CreateRecord(1, new DateTimeOffset(2026, 7, 27, 16, 0, 0, TimeSpan.Zero)),
            CreateRecord(2, new DateTimeOffset(2026, 7, 28, 15, 59, 59, TimeSpan.Zero)),
            CreateRecord(3, new DateTimeOffset(2026, 7, 28, 16, 0, 0, TimeSpan.Zero)));
        await dbContext.SaveChangesAsync();
        var service = new DailyRecordService(
            dbContext,
            new LocalizationCurrentUserService(),
            TimeProvider.System);

        // Act
        var records = await service.GetDailyRecordsAsync(
            new DateOnly(2026, 7, 28),
            "Asia/Taipei",
            "en-US");

        // Assert
        Assert.That(records.Select(record => record.RecordId), Is.EqualTo(new[] { 1L, 2L }));
        Assert.That(records.All(record => record.Food.DisplayName == "Test food"), Is.True);
        Assert.That(records.All(record => record.Food.LangCode == "en-US"), Is.True);
        Assert.That(records.All(record =>
            record.Nutrients.Single().DisplayName == "Protein"), Is.True);
        Assert.That(records.All(record =>
            record.Nutrients.Single().LangCode == "en-US"), Is.True);
    }

    private static DailyRecord CreateRecord(long id, DateTimeOffset consumedAt)
    {
        return new DailyRecord
        {
            RecordId = id,
            UserId = 42,
            FoodId = 1,
            Quantity = 100,
            ConsumedAt = consumedAt,
            MealTypeCode = "Snack",
        };
    }

    private sealed class LocalizationCurrentUserService : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public long? UserId => 42;
        public string? UserName => null;
    }
}
