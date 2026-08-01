using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
/// 驗證 Body Profile 的正式 HTTP 契約、授權與錯誤回應。
/// </summary>
public sealed class BodyProfilesApiTests
{
    private const string Path = "/api/me/body-profile";
    private const string TestAuthenticationScheme = "Test";

    [Test]
    public async Task Get_WhenAnonymous_ReturnsUnauthorized()
    {
        await using var factory = new BodyProfileApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(Path);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Get_WhenProfileDoesNotExist_ReturnsCodeFirstNotFound()
    {
        await using var factory = new BodyProfileApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync(Path);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(json.RootElement.GetProperty("code").GetString(),
                Is.EqualTo("BodyProfile.NotFound"));
            Assert.That(json.RootElement.GetProperty("traceId").GetString(),
                Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task PutThenGet_WhenRequestIsValid_PersistsCompleteProfile()
    {
        await using var factory = new BodyProfileApiFactory();
        await factory.SeedAsync();
        using var client = CreateAuthenticatedClient(factory);

        var putResponse = await client.PutAsJsonAsync(Path, CreateRequest());
        var getResponse = await client.GetAsync($"{Path}?langCode=fr-FR");
        using var json = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(json.RootElement.GetProperty("birthDate").GetString(),
                Is.EqualTo("1990-05-20"));
            Assert.That(json.RootElement.GetProperty("biologicalSexCode").GetString(),
                Is.EqualTo("MALE"));
            Assert.That(json.RootElement.GetProperty("version").GetGuid(),
                Is.Not.EqualTo(Guid.Empty));
            Assert.That(json.RootElement.GetProperty("fitnessGoalDisplayName").GetString(),
                Is.EqualTo("Maintain weight"));
            Assert.That(json.RootElement.GetProperty("fitnessGoalLangCode").GetString(),
                Is.EqualTo("en-US"));
            Assert.That(json.RootElement.GetProperty("fitnessGoalNote").GetString(),
                Is.EqualTo("Keep current weight."));
        });
    }

    [Test]
    public async Task Get_WhenLangCodeIsInvalid_ReturnsFieldValidationError()
    {
        await using var factory = new BodyProfileApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync($"{Path}?langCode=invalid_tag");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(json.RootElement.GetProperty("errors")
                .GetProperty("langCode")[0].GetProperty("code").GetString(),
                Is.EqualTo("DefinedCode.InvalidLangCode"));
        });
    }

    [Test]
    public async Task Put_WhenTimeZoneIsInvalid_ReturnsFieldValidationError()
    {
        await using var factory = new BodyProfileApiFactory();
        await factory.SeedAsync();
        using var client = CreateAuthenticatedClient(factory);
        var request = CreateRequest();
        request.TimeZone = "Taipei Standard Time";

        var response = await client.PutAsJsonAsync(Path, request);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(json.RootElement.GetProperty("errors")
                .GetProperty("timeZone")[0].GetProperty("code").GetString(),
                Is.EqualTo("BodyProfile.InvalidTimeZone"));
            Assert.That(json.RootElement.GetProperty("traceId").GetString(),
                Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task Put_WhenVersionIsStale_ReturnsConflict()
    {
        await using var factory = new BodyProfileApiFactory();
        await factory.SeedAsync();
        using var client = CreateAuthenticatedClient(factory);
        var createResponse = await client.PutAsJsonAsync(Path, CreateRequest());
        using var createdJson = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());
        var firstVersion = createdJson.RootElement.GetProperty("version").GetGuid();
        var update = CreateRequest(firstVersion);
        update.HeightInCentimeters = 180;
        await client.PutAsJsonAsync(Path, update);
        var staleUpdate = CreateRequest(firstVersion);
        staleUpdate.HeightInCentimeters = 190;

        var response = await client.PutAsJsonAsync(Path, staleUpdate);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(json.RootElement.GetProperty("code").GetString(),
                Is.EqualTo("BodyProfile.Conflict"));
            Assert.That(json.RootElement.GetProperty("traceId").GetString(),
                Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task PutThenGet_WhenTwoUsersAreAuthenticated_IsolatesProfiles()
    {
        await using var factory = new BodyProfileApiFactory();
        await factory.SeedAsync();
        using var firstClient = CreateAuthenticatedClient(factory, 42);
        using var secondClient = CreateAuthenticatedClient(factory, 43);
        var secondRequest = CreateRequest();
        secondRequest.HeightInCentimeters = 188m;

        await firstClient.PutAsJsonAsync(Path, CreateRequest());
        await secondClient.PutAsJsonAsync(Path, secondRequest);
        var first = await firstClient.GetFromJsonAsync<JsonElement>(Path);
        var second = await secondClient.GetFromJsonAsync<JsonElement>(Path);

        Assert.Multiple(() =>
        {
            Assert.That(first.GetProperty("heightInCentimeters").GetDecimal(),
                Is.EqualTo(175.5m));
            Assert.That(second.GetProperty("heightInCentimeters").GetDecimal(),
                Is.EqualTo(188m));
        });
    }

    private static TestRequest CreateRequest(Guid? version = null) => new()
    {
        BirthDate = "1990-05-20",
        BiologicalSexCode = "MALE",
        HeightInCentimeters = 175.5m,
        FitnessGoalCode = "MAINTAIN",
        ActivityLevelCode = "MODERATE",
        TimeZone = "Asia/Taipei",
        Version = version,
    };

    private static HttpClient CreateAuthenticatedClient(
        BodyProfileApiFactory factory,
        long userId = 42)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            userId.ToString());
        return client;
    }

    private sealed class TestRequest
    {
        public required string BirthDate { get; set; }
        public required string BiologicalSexCode { get; set; }
        public decimal HeightInCentimeters { get; set; }
        public required string FitnessGoalCode { get; set; }
        public required string ActivityLevelCode { get; set; }
        public required string TimeZone { get; set; }
        public Guid? Version { get; set; }
    }

    private sealed class BodyProfileApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"BodyProfileApiTests-{Guid.NewGuid()}";

        public async Task SeedAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.DefinedCodes.AddRange(
                new DefinedCode
                {
                    CodeType = DefinedCodeTypes.FitnessGoal,
                    Code = "MAINTAIN",
                    SortOrder = 1,
                    IsActive = true,
                    Translations =
                    [
                        new DefinedCodeTranslation
                        {
                            CodeType = DefinedCodeTypes.FitnessGoal,
                            Code = "MAINTAIN",
                            LangCode = "en-US",
                            DisplayName = "Maintain weight",
                            Note = "Keep current weight.",
                        },
                    ],
                },
                new DefinedCode
                {
                    CodeType = DefinedCodeTypes.ActivityLevel,
                    Code = "MODERATE",
                    SortOrder = 1,
                    IsActive = true,
                });
            await dbContext.SaveChangesAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddAuthentication(options =>
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
                    AuthenticationHeaderValue.TryParse(header, out var value)
                    && value.Scheme == TestAuthenticationScheme))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var authorization = AuthenticationHeaderValue.Parse(
                Request.Headers.Authorization.ToString());
            var userId = long.TryParse(authorization.Parameter, out var parsedUserId)
                ? parsedUserId
                : 42;
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                TestAuthenticationScheme);
            var ticket = new AuthenticationTicket(
                new ClaimsPrincipal(identity),
                TestAuthenticationScheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
