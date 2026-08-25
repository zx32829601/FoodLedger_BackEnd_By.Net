using FoodLedger.Data.Entities;
using FoodLedger.Services;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Tests.Services;

/// <summary>
/// 驗證營養素目錄依指定語系提供建立食物表單所需的標籤與單位。
/// </summary>
[Category("Nutrition")]
[Category("Unit")]
public sealed class NutrientCatalogServiceTests
{
    /// <summary>
    /// 驗證指定語系優先、英文 fallback，且無翻譯時仍以穩定代碼回傳營養素。
    /// </summary>
    [Test]
    public async Task GetAsync_WhenTranslationsVary_ReturnsLocalizedCatalogWithoutDroppingNutrients()
    {
        // Arrange
        await using var dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"NutrientCatalog-{Guid.NewGuid()}")
                .Options);
        dbContext.Nutrients.AddRange(
            CreateNutrient(1, "Protein", "g", ("zh-TW", "蛋白質"), ("en-US", "Protein")),
            CreateNutrient(2, "Calories", "kcal", ("en-US", "Calories")),
            CreateNutrient(3, "Sodium", "mg"));
        await dbContext.SaveChangesAsync();
        var service = new NutrientCatalogService(dbContext);

        // Act
        var response = await service.GetAsync("zh-TW");

        // Assert
        Assert.That(
            response.Select(item => (item.Code, item.DisplayName, item.LangCode, item.UnitCode)),
            Is.EqualTo(new[]
            {
                ("Calories", "Calories", "en-US", "kcal"),
                ("Protein", "蛋白質", "zh-TW", "g"),
                ("Sodium", "Sodium", (string?)null, "mg"),
            }));
    }

    private static Nutrient CreateNutrient(
        long nutrientId,
        string code,
        string unitCode,
        params (string LangCode, string DisplayName)[] translations)
    {
        return new Nutrient
        {
            NutrientId = nutrientId,
            NutrientCode = code,
            UnitCode = unitCode,
            Translations = translations
                .Select((translation, index) => new NutrientTranslation
                {
                    TranslationId = nutrientId * 10 + index,
                    LangCode = translation.LangCode,
                    NutrientName = translation.DisplayName,
                })
                .ToArray(),
        };
    }
}
