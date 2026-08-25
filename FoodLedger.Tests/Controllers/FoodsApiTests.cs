using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using FoodLedger.Data.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodLedger.Tests.Controllers;

/// <summary>
/// 驗證食物搜尋 API 的查詢、翻譯、分頁與營養資料契約。
/// </summary>
[Category("Foods")]
[Category("Integration")]
public class FoodsApiTests
{
    private const string TestAuthenticationScheme = "Test";

    /// <summary>
    /// 驗證已登入使用者以繁體中文搜尋食物時，取得分頁且依名稱穩定排序的結果。
    /// </summary>
    [Test]
    public async Task SearchAsync_WhenZhTwFoodsMatch_ReturnsPagedFoodsOrderedByDisplayName()
    {
        // Arrange
        await using var factory = new FoodsApiFactory();
        await factory.SeedAsync(dbContext =>
        {
            dbContext.SimpleFoods.AddRange(
                CreateFood(1, "FRUIT_A", "果A"),
                CreateFood(2, "FRUIT_B", "果B"));
        });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");

        // Act
        var response = await client.GetAsync(
            "/api/foods?query=果&langCode=zh-tw&page=1&pageSize=1");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        var root = json.RootElement;
        var items = root.GetProperty("items");
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(root.GetProperty("page").GetInt32(), Is.EqualTo(1));
            Assert.That(root.GetProperty("pageSize").GetInt32(), Is.EqualTo(1));
            Assert.That(root.GetProperty("totalCount").GetInt32(), Is.EqualTo(2));
            Assert.That(items.GetArrayLength(), Is.EqualTo(1));
            Assert.That(items[0].GetProperty("foodId").GetInt64(), Is.EqualTo(1));
            Assert.That(items[0].GetProperty("foodCode").GetString(), Is.EqualTo("FRUIT_A"));
            Assert.That(items[0].GetProperty("displayName").GetString(), Is.EqualTo("果A"));
            Assert.That(items[0].GetProperty("langCode").GetString(), Is.EqualTo("zh-TW"));
        });
    }

    /// <summary>
    /// 驗證缺少指定語系時使用英文翻譯，且指定語系與英文皆缺少的食物不會回傳。
    /// </summary>
    [Test]
    public async Task SearchAsync_WhenRequestedTranslationIsMissing_UsesEnglishFallbackAndExcludesUntranslatedFood()
    {
        // Arrange
        await using var factory = new FoodsApiFactory();
        await factory.SeedAsync(dbContext =>
        {
            dbContext.SimpleFoods.AddRange(
                new SimpleFood
                {
                    FoodId = 1,
                    FoodCode = "CHICKEN",
                    Translations =
                    [
                        new SimpleFoodTranslation
                        {
                            TranslationId = 1,
                            LangCode = "en-US",
                            FoodName = "Chicken",
                        },
                    ],
                },
                new SimpleFood
                {
                    FoodId = 2,
                    FoodCode = "FRENCH_ONLY",
                    Translations =
                    [
                        new SimpleFoodTranslation
                        {
                            TranslationId = 2,
                            LangCode = "fr-FR",
                            FoodName = "Poulet",
                        },
                    ],
                });
        });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");

        // Act
        var response = await client.GetAsync("/api/foods?query=Chicken&langCode=zh-TW");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        var items = json.RootElement.GetProperty("items");
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(json.RootElement.GetProperty("totalCount").GetInt32(), Is.EqualTo(1));
            Assert.That(items.GetArrayLength(), Is.EqualTo(1));
            Assert.That(items[0].GetProperty("foodCode").GetString(), Is.EqualTo("CHICKEN"));
            Assert.That(items[0].GetProperty("displayName").GetString(), Is.EqualTo("Chicken"));
            Assert.That(items[0].GetProperty("langCode").GetString(), Is.EqualTo("en-US"));
        });
    }

    /// <summary>
    /// 驗證食物搜尋只回傳輕量的每 100 克熱量摘要，不夾帶完整營養素。
    /// </summary>
    [Test]
    public async Task SearchAsync_WhenFoodHasNutrients_ReturnsOnlyCalorieSummary()
    {
        // Arrange
        await using var factory = new FoodsApiFactory();
        await factory.SeedAsync(dbContext =>
        {
            dbContext.SimpleFoods.Add(CreateFood(1, "CHICKEN", "雞胸肉"));
            dbContext.Nutrients.AddRange(
                new Nutrient
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
                    ],
                },
                new Nutrient
                {
                    NutrientId = 2,
                    NutrientCode = "Calories",
                    UnitCode = "kcal",
                    Translations =
                    [
                        new NutrientTranslation
                        {
                            TranslationId = 2,
                            LangCode = "en-US",
                            NutrientName = "Calories",
                        },
                    ],
                });
            dbContext.FoodNutrients.AddRange(
                new FoodNutrient { FoodId = 1, NutrientId = 1, Amount = 31.25m },
                new FoodNutrient { FoodId = 1, NutrientId = 2, Amount = 165m });
        });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");

        // Act
        var response = await client.GetAsync("/api/foods?query=雞&langCode=zh-TW");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        var item = json.RootElement.GetProperty("items")[0];
        var calories = item.GetProperty("caloriesPer100Grams");
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(calories.GetDecimal(), Is.EqualTo(165m));
            Assert.That(item.TryGetProperty("nutrients", out _), Is.False);
        });
    }

    /// <summary>
    /// 驗證食物明細包含雙語名稱、說明、分類，以及依顯示順序排列的完整營養資料。
    /// </summary>
    [Test]
    public async Task GetAsync_WhenFoodExists_ReturnsLocalizedDetailOrderedByDisplayOrder()
    {
        await using var factory = new FoodsApiFactory();
        await factory.SeedAsync(dbContext =>
        {
            dbContext.SimpleFoods.Add(new SimpleFood
            {
                FoodId = 1,
                FoodCode = "CHICKEN",
                Translations =
                [
                    new SimpleFoodTranslation { TranslationId = 1, LangCode = "zh-TW", FoodName = "雞胸肉", Description = "低脂蛋白質來源" },
                    new SimpleFoodTranslation { TranslationId = 2, LangCode = "en-US", FoodName = "Chicken Breast" },
                ],
            });
            dbContext.FoodCategories.Add(new FoodCategory
            {
                CategoryId = 1,
                CategoryCode = "MEAT",
                Translations =
                [
                    new FoodCategoryTranslation { TranslationId = 1, LangCode = "zh-TW", CategoryName = "肉類" },
                ],
            });
            dbContext.SimpleFoodCategories.Add(new SimpleFoodCategory { FoodId = 1, CategoryId = 1 });
            dbContext.Nutrients.AddRange(
                new Nutrient { NutrientId = 1, NutrientCode = "Sodium", UnitCode = "mg", DisplayOrder = 50 },
                new Nutrient { NutrientId = 2, NutrientCode = "Protein", UnitCode = "g", DisplayOrder = 20 });
            dbContext.FoodNutrients.AddRange(
                new FoodNutrient { FoodId = 1, NutrientId = 1, Amount = 74m },
                new FoodNutrient { FoodId = 1, NutrientId = 2, Amount = 31m });
        });
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync("/api/foods/1?langCode=zh-TW");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(root.GetProperty("displayName").GetString(), Is.EqualTo("雞胸肉"));
            Assert.That(root.GetProperty("englishName").GetString(), Is.EqualTo("Chicken Breast"));
            Assert.That(root.GetProperty("description").GetString(), Is.EqualTo("低脂蛋白質來源"));
            Assert.That(root.GetProperty("categories")[0].GetProperty("displayName").GetString(), Is.EqualTo("肉類"));
            Assert.That(root.GetProperty("nutrients")[0].GetProperty("code").GetString(), Is.EqualTo("Protein"));
            Assert.That(root.GetProperty("nutrients")[0].GetProperty("displayOrder").GetInt32(), Is.EqualTo(20));
            Assert.That(root.GetProperty("nutrients")[0].GetProperty("displayName").GetString(), Is.EqualTo("Protein"));
            Assert.That(root.GetProperty("nutrients")[0].GetProperty("langCode").ValueKind, Is.EqualTo(JsonValueKind.Null));
        });
    }

    /// <summary>
    /// 驗證空白搜尋文字會回傳指定語系可顯示的全部食物。
    /// </summary>
    [Test]
    public async Task SearchAsync_WhenQueryIsBlank_ReturnsAllDisplayableFoods()
    {
        // Arrange
        await using var factory = new FoodsApiFactory();
        await factory.SeedAsync(dbContext =>
        {
            dbContext.SimpleFoods.AddRange(
                CreateFood(1, "FOOD_B", "食物B"),
                CreateFood(2, "FOOD_A", "食物A"));
        });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");

        // Act
        var response = await client.GetAsync("/api/foods?query=%20%20%20");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = json.RootElement.GetProperty("items");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(json.RootElement.GetProperty("totalCount").GetInt32(), Is.EqualTo(2));
            Assert.That(items.GetArrayLength(), Is.EqualTo(2));
            Assert.That(items[0].GetProperty("foodCode").GetString(), Is.EqualTo("FOOD_A"));
            Assert.That(items[1].GetProperty("foodCode").GetString(), Is.EqualTo("FOOD_B"));
        });
    }

    /// <summary>
    /// 驗證未登入使用者不可搜尋食物。
    /// </summary>
    [Test]
    public async Task SearchAsync_WhenRequestIsAnonymous_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new FoodsApiFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/foods?query=雞");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>
    /// 驗證無效語系與分頁條件會回傳對應欄位的穩定錯誤代碼。
    /// </summary>
    /// <param name="queryString">測試用 query string。</param>
    /// <param name="fieldName">預期發生錯誤的欄位名稱。</param>
    /// <param name="errorCode">預期的穩定錯誤代碼。</param>
    [TestCase(
        "query=food&langCode=invalid_tag",
        "langCode",
        "FoodSearch.InvalidLangCode")]
    [TestCase("query=food&page=0", "page", "FoodSearch.PageOutOfRange")]
    [TestCase(
        "query=food&pageSize=101",
        "pageSize",
        "FoodSearch.PageSizeOutOfRange")]
    public async Task SearchAsync_WhenQueryParametersAreInvalid_ReturnsFieldValidationError(
        string queryString,
        string fieldName,
        string errorCode)
    {
        // Arrange
        await using var factory = new FoodsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");

        // Act
        var response = await client.GetAsync($"/api/foods?{queryString}");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var fieldErrors = json.RootElement.GetProperty("errors").GetProperty(fieldName);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(fieldErrors[0].GetProperty("code").GetString(), Is.EqualTo(errorCode));
        });
    }

    /// <summary>
    /// 驗證未指定分頁條件時使用第一頁與每頁二十筆。
    /// </summary>
    [Test]
    public async Task SearchAsync_WhenPaginationIsOmitted_UsesDefaultPagination()
    {
        // Arrange
        await using var factory = new FoodsApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.GetAsync("/api/foods?query=food");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(json.RootElement.GetProperty("page").GetInt32(), Is.EqualTo(1));
            Assert.That(json.RootElement.GetProperty("pageSize").GetInt32(), Is.EqualTo(20));
        });
    }

    /// <summary>
    /// 驗證每頁一百筆的上限值仍是有效的查詢條件。
    /// </summary>
    [Test]
    public async Task SearchAsync_WhenPageSizeIsMaximum_ReturnsOk()
    {
        // Arrange
        await using var factory = new FoodsApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.GetAsync("/api/foods?query=food&pageSize=100");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// 驗證沒有符合結果時回傳空集合，而不是錯誤狀態。
    /// </summary>
    [Test]
    public async Task SearchAsync_WhenNoFoodMatches_ReturnsEmptyPage()
    {
        // Arrange
        await using var factory = new FoodsApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.GetAsync("/api/foods?query=missing");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(json.RootElement.GetProperty("totalCount").GetInt32(), Is.Zero);
            Assert.That(json.RootElement.GetProperty("items").GetArrayLength(), Is.Zero);
        });
    }

    /// <summary>
    /// 驗證含 script subtag 的合法 BCP 47 語系代碼可以進入搜尋流程。
    /// </summary>
    [TestCase("zh-Hant-TW")]
    [TestCase("x-private")]
    public async Task SearchAsync_WhenLangCodeIsValidBcp47Tag_ReturnsOk(string langCode)
    {
        // Arrange
        await using var factory = new FoodsApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.GetAsync($"/api/foods?query=food&langCode={langCode}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// 驗證 BCP 47 語系代碼比對不區分大小寫。
    /// </summary>
    [Test]
    public async Task SearchAsync_WhenPrivateUseLangCodeCasingDiffers_ReturnsRequestedTranslation()
    {
        // Arrange
        await using var factory = new FoodsApiFactory();
        await factory.SeedAsync(dbContext =>
        {
            dbContext.SimpleFoods.Add(new SimpleFood
            {
                FoodId = 1,
                FoodCode = "PRIVATE_FOOD",
                Translations =
                [
                    new SimpleFoodTranslation
                    {
                        TranslationId = 1,
                        LangCode = "x-private",
                        FoodName = "Private Food",
                    },
                ],
            });
        });
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.GetAsync(
            "/api/foods?query=Private&langCode=X-PRIVATE");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(json.RootElement.GetProperty("totalCount").GetInt32(), Is.EqualTo(1));
            Assert.That(
                json.RootElement.GetProperty("items")[0].GetProperty("langCode").GetString(),
                Is.EqualTo("x-private"));
        });
    }

    private static SimpleFood CreateFood(long foodId, string foodCode, string foodName)
    {
        return new SimpleFood
        {
            FoodId = foodId,
            FoodCode = foodCode,
            Translations =
            [
                new SimpleFoodTranslation
                {
                    TranslationId = foodId,
                    LangCode = "zh-TW",
                    FoodName = foodName,
                },
            ],
        };
    }

    private static HttpClient CreateAuthenticatedClient(FoodsApiFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");
        return client;
    }

    private sealed class FoodsApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"FoodsApiTests-{Guid.NewGuid()}";

        public async Task SeedAsync(Action<ApplicationDbContext> seed)
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
            seed(dbContext);
            await dbContext.SaveChangesAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services
                    .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthenticationScheme;
                        options.DefaultChallengeScheme = TestAuthenticationScheme;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        TestAuthenticationScheme,
                        options => { });
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));
            });
        }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.Authorization.Any(header =>
                    AuthenticationHeaderValue.TryParse(header, out var authenticationHeader)
                    && authenticationHeader.Scheme == TestAuthenticationScheme))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "42")],
                TestAuthenticationScheme);
            var ticket = new AuthenticationTicket(
                new ClaimsPrincipal(identity),
                TestAuthenticationScheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
