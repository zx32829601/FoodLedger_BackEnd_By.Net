using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
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
/// 驗證 Nutrition Summary 與營養素目錄的正式 HTTP 契約。
/// </summary>
[Category("Nutrition")]
[Category("Integration")]
public sealed class NutritionSummaryApiTests
{
    private const string TestAuthenticationScheme = "Test";

    /// <summary>
    /// 驗證 daily endpoint 接收本地日期、IANA timezone 與語系並回傳空摘要。
    /// </summary>
    [Test]
    public async Task GetDailyAsync_WhenRequestIsValid_ReturnsLocalizedEmptySummary()
    {
        // Arrange
        await using var factory = new NutritionSummaryApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.GetAsync(
            "/api/nutrition-summary/daily"
            + "?date=2026-07-28&timeZone=Asia%2FTaipei&langCode=zh-TW");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(json.RootElement.GetProperty("date").GetString(), Is.EqualTo("2026-07-28"));
            Assert.That(
                json.RootElement.GetProperty("timeZone").GetString(),
                Is.EqualTo("Asia/Taipei"));
            Assert.That(json.RootElement.GetProperty("totals").GetArrayLength(), Is.Zero);
            Assert.That(json.RootElement.GetProperty("mealTypes").GetArrayLength(), Is.Zero);
        });
    }

    /// <summary>
    /// 驗證 weekly endpoint 回傳焦點日期所在週及固定七天 breakdown。
    /// </summary>
    [Test]
    public async Task GetWeeklyAsync_WhenRequestIsValid_ReturnsSevenDayEmptySummary()
    {
        // Arrange
        await using var factory = new NutritionSummaryApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.GetAsync(
            "/api/nutrition-summary/weekly"
            + "?date=2026-07-29&timeZone=Asia%2FTaipei&langCode=zh-TW");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(
                json.RootElement.GetProperty("startDate").GetString(),
                Is.EqualTo("2026-07-27"));
            Assert.That(
                json.RootElement.GetProperty("endDate").GetString(),
                Is.EqualTo("2026-08-02"));
            Assert.That(json.RootElement.GetProperty("days").GetArrayLength(), Is.EqualTo(7));
        });
    }

    /// <summary>
    /// 驗證無效時區會回傳含穩定錯誤代碼的 ValidationProblem。
    /// </summary>
    [Test]
    public async Task GetDailyAsync_WhenTimeZoneIsInvalid_ReturnsValidationProblem()
    {
        // Arrange
        await using var factory = new NutritionSummaryApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.GetAsync(
            "/api/nutrition-summary/daily"
            + "?date=2026-07-28&timeZone=Not%2FA-TimeZone&langCode=zh-TW");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(
                json.RootElement.GetProperty("code").GetString(),
                Is.EqualTo("Validation.Failed"));
            Assert.That(
                json.RootElement
                    .GetProperty("errors")
                    .GetProperty("timeZone")[0]
                    .GetProperty("code")
                    .GetString(),
                Is.EqualTo("NutritionSummary.InvalidTimeZone"));
        });
    }

    /// <summary>
    /// 驗證 Windows timezone ID 不符合 IANA 契約並回傳 400。
    /// </summary>
    [Test]
    public async Task GetDailyAsync_WhenTimeZoneUsesWindowsId_ReturnsValidationProblem()
    {
        // Arrange
        await using var factory = new NutritionSummaryApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.GetAsync(
            "/api/nutrition-summary/daily"
            + "?date=2026-07-28&timeZone=Taipei%20Standard%20Time&langCode=zh-TW");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// 驗證無效 BCP 47 語系會回傳含穩定錯誤代碼的 ValidationProblem。
    /// </summary>
    [Test]
    public async Task GetDailyAsync_WhenLangCodeIsInvalid_ReturnsValidationProblem()
    {
        // Arrange
        await using var factory = new NutritionSummaryApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.GetAsync(
            "/api/nutrition-summary/daily"
            + "?date=2026-07-28&timeZone=Asia%2FTaipei&langCode=not_a_lang");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(
                json.RootElement
                    .GetProperty("errors")
                    .GetProperty("langCode")[0]
                    .GetProperty("code")
                    .GetString(),
                Is.EqualTo("NutritionSummary.InvalidLangCode"));
        });
    }

    /// <summary>
    /// 驗證營養素目錄 endpoint 回傳指定語系的目錄陣列。
    /// </summary>
    [Test]
    public async Task GetNutrientsAsync_WhenRequestIsValid_ReturnsLocalizedCatalog()
    {
        // Arrange
        await using var factory = new NutritionSummaryApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.GetAsync("/api/nutrients?langCode=zh-TW");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(json.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array));
        });
    }

    /// <summary>
    /// 驗證未登入 request 無法讀取每日營養摘要。
    /// </summary>
    [Test]
    public async Task GetDailyAsync_WhenAnonymous_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new NutritionSummaryApiFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/nutrition-summary/daily"
            + "?date=2026-07-28&timeZone=Asia%2FTaipei&langCode=zh-TW");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>
    /// 驗證未登入 request 無法讀取營養素目錄。
    /// </summary>
    [Test]
    public async Task GetNutrientsAsync_WhenAnonymous_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new NutritionSummaryApiFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/nutrients?langCode=zh-TW");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private static HttpClient CreateAuthenticatedClient(NutritionSummaryApiFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");
        return client;
    }

    private sealed class NutritionSummaryApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"NutritionSummaryApiTests-{Guid.NewGuid()}";

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
