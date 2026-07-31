using System.Net;
using System.Net.Http.Json;
using FoodLedger.DTOs.DefinedCodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodLedger.Tests.Controllers;

/// <summary>
/// 驗證 DefinedCode 公開 API 的 HTTP 契約。
/// </summary>
public class DefinedCodesApiTests
{
    /// <summary>
    /// 驗證未登入使用者可取得啟用餐別，且 response 依排序值排列。
    /// </summary>
    [Test]
    public async Task GetMealTypes_WhenRequestIsAnonymous_ReturnsActiveMealTypesOrderedBySortOrder()
    {
        // Arrange
        await using var factory = new DefinedCodesApiFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/defined-codes/meal-types");
        var mealTypes = await response.Content.ReadFromJsonAsync<List<DefinedCodeResponse>>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(
                mealTypes?.Select(mealType => mealType.Code),
                Is.EqualTo(new[] { "Breakfast", "Lunch", "Dinner", "Snack" }));
            Assert.That(
                mealTypes?.Select(mealType => mealType.SortOrder),
                Is.EqualTo(new[] { 1, 2, 3, 4 }));
        });
    }

    /// <summary>
    /// 驗證指定英文語系時，餐別 API 回傳英文顯示名稱、說明與實際採用語系。
    /// </summary>
    [Test]
    public async Task GetMealTypes_WhenEnglishIsRequested_ReturnsEnglishTranslationAndNote()
    {
        // Arrange
        await using var factory = new DefinedCodesApiFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/defined-codes/meal-types?langCode=en-US");
        var mealTypes = await response.Content.ReadFromJsonAsync<List<DefinedCodeResponse>>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(mealTypes?.First().DisplayName, Is.EqualTo("Breakfast"));
            Assert.That(
                mealTypes?.First().Note,
                Is.EqualTo("The first meal of the day, typically eaten in the morning."));
            Assert.That(mealTypes?.First().LangCode, Is.EqualTo("en-US"));
        });
    }

    /// <summary>
    /// 驗證指定語系沒有翻譯時，餐別 API 使用 en-US 翻譯作為 fallback。
    /// </summary>
    [Test]
    public async Task GetMealTypes_WhenRequestedTranslationDoesNotExist_FallsBackToEnglish()
    {
        // Arrange
        await using var factory = new DefinedCodesApiFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/defined-codes/meal-types?langCode=fr-FR");
        var mealTypes = await response.Content.ReadFromJsonAsync<List<DefinedCodeResponse>>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(mealTypes?.First().DisplayName, Is.EqualTo("Breakfast"));
            Assert.That(mealTypes?.First().LangCode, Is.EqualTo("en-US"));
        });
    }

    /// <summary>
    /// 驗證未登入使用者可取得已在地化並依順序排列的 Fitness Goal。
    /// </summary>
    [Test]
    public async Task GetFitnessGoals_WhenTraditionalChineseIsRequested_ReturnsLocalizedGoals()
    {
        // Arrange
        await using var factory = new DefinedCodesApiFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/defined-codes/fitness-goals?langCode=zh-TW");
        var goals = await response.Content.ReadFromJsonAsync<List<DefinedCodeResponse>>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(
                goals?.Select(goal => goal.Code),
                Is.EqualTo(new[] { "FAT_LOSS", "MAINTAIN", "MUSCLE_GAIN" }));
            Assert.That(
                goals?.Select(goal => goal.DisplayName),
                Is.EqualTo(new[] { "減脂", "維持體重", "增肌" }));
            Assert.That(goals?.All(goal => !string.IsNullOrWhiteSpace(goal.Note)), Is.True);
        });
    }

    /// <summary>
    /// 驗證活動程度 API 會依排序回傳完整代碼、翻譯與說明。
    /// </summary>
    [Test]
    public async Task GetActivityLevels_WhenEnglishIsRequested_ReturnsLocalizedLevels()
    {
        // Arrange
        await using var factory = new DefinedCodesApiFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/defined-codes/activity-levels?langCode=en-US");
        var levels = await response.Content.ReadFromJsonAsync<List<DefinedCodeResponse>>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(
                levels?.Select(level => level.Code),
                Is.EqualTo(new[] { "SEDENTARY", "LIGHT", "MODERATE", "HIGH", "VERY_HIGH" }));
            Assert.That(
                levels?.Select(level => level.DisplayName),
                Is.EqualTo(new[]
                {
                    "Sedentary",
                    "Lightly active",
                    "Moderately active",
                    "Highly active",
                    "Very highly active",
                }));
            Assert.That(levels?.All(level => !string.IsNullOrWhiteSpace(level.Note)), Is.True);
        });
    }

    /// <summary>
    /// 驗證不合法的語系代碼會回傳 DefinedCode 專屬的驗證錯誤。
    /// </summary>
    [Test]
    public async Task GetMealTypes_WhenLangCodeIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        await using var factory = new DefinedCodesApiFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/defined-codes/meal-types?langCode=invalid_language");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private sealed class DefinedCodesApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"DefinedCodesApiTests-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));
            });
        }
    }
}
