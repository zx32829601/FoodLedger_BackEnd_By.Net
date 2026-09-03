using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

/// <summary>驗證 Body Measurement 的授權、HTTP 契約與完整刪除流程。</summary>
[Category("BodyMeasurements")]
[Category("Integration")]
public sealed class BodyMeasurementsApiTests
{
    private const string Path = "/api/me/body-measurements";
    private const string TestAuthenticationScheme = "Test";
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task GetHistory_WhenAnonymous_ReturnsUnauthorized()
    {
        await using var factory = new BodyMeasurementApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(Path);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PostThenGetHistory_WhenValid_ReturnsCreatedServerTimeAndPagedHistory()
    {
        await using var factory = new BodyMeasurementApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var createResponse = await client.PostAsJsonAsync(Path, new
        {
            weightInKilograms = 72.35m,
            bodyFatPercentage = 18.4m,
            muscleMassInKilograms = 31.25m,
        });
        var historyResponse = await client.GetAsync(Path);
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        using var history = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(createResponse.Headers.Location?.OriginalString,
                Does.StartWith(Path + "/"));
            Assert.That(created.RootElement.GetProperty("measuredAt").GetDateTimeOffset(),
                Is.EqualTo(FixedUtcNow));
            Assert.That(historyResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(history.RootElement.GetProperty("page").GetInt32(), Is.EqualTo(1));
            Assert.That(history.RootElement.GetProperty("pageSize").GetInt32(), Is.EqualTo(20));
            Assert.That(history.RootElement.GetProperty("totalCount").GetInt32(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Post_WhenWeightIsInvalid_ReturnsCodeFirstFieldError()
    {
        await using var factory = new BodyMeasurementApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync(Path, new
        {
            weightInKilograms = 19.99m,
        });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(json.RootElement.GetProperty("errors")
                .GetProperty("weightInKilograms")[0].GetProperty("code").GetString(),
                Is.EqualTo("BodyMeasurement.WeightOutOfRange"));
            Assert.That(json.RootElement.GetProperty("traceId").GetString(),
                Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task GetHistory_WhenPageSizeExceedsMaximum_ReturnsFieldValidationError()
    {
        await using var factory = new BodyMeasurementApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync($"{Path}?pageSize=101");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(json.RootElement.GetProperty("errors")
                .GetProperty("pageSize")[0].GetProperty("code").GetString(),
                Is.EqualTo("BodyMeasurement.PageSizeOutOfRange"));
        });
    }

    [Test]
    public async Task Put_WhenVersionIsStale_ReturnsConflict()
    {
        await using var factory = new BodyMeasurementApiFactory();
        using var client = CreateAuthenticatedClient(factory);
        var created = await CreateMeasurementAsync(client);
        var measurementId = created.GetProperty("measurementId").GetInt64();

        var response = await client.PutAsJsonAsync($"{Path}/{measurementId}", new
        {
            weightInKilograms = 73m,
            version = Guid.NewGuid(),
        });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(json.RootElement.GetProperty("code").GetString(),
                Is.EqualTo("BodyMeasurement.Conflict"));
        });
    }

    [Test]
    public async Task Put_ForAnotherUsersMeasurement_ReturnsMaskedNotFound()
    {
        await using var factory = new BodyMeasurementApiFactory();
        using var ownerClient = CreateAuthenticatedClient(factory, 42);
        using var otherClient = CreateAuthenticatedClient(factory, 43);
        var created = await CreateMeasurementAsync(ownerClient);
        var measurementId = created.GetProperty("measurementId").GetInt64();

        var response = await otherClient.PutAsJsonAsync($"{Path}/{measurementId}", new
        {
            weightInKilograms = 73m,
            version = created.GetProperty("version").GetGuid(),
        });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(json.RootElement.GetProperty("code").GetString(),
                Is.EqualTo("BodyMeasurement.NotFound"));
        });
    }

    [Test]
    public async Task GetHistory_WithDateFilterAndNoProfile_ReturnsProfileRequired()
    {
        await using var factory = new BodyMeasurementApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync($"{Path}?fromDate=2026-08-25");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
            Assert.That(json.RootElement.GetProperty("code").GetString(),
                Is.EqualTo("BodyMeasurement.ProfileRequired"));
        });
    }

    [Test]
    public async Task DeletionImpactThenDelete_WithCurrentVersionAndToken_RemovesRecord()
    {
        await using var factory = new BodyMeasurementApiFactory();
        using var client = CreateAuthenticatedClient(factory);
        var created = await CreateMeasurementAsync(client);
        var measurementId = created.GetProperty("measurementId").GetInt64();
        var impactResponse = await client.GetAsync($"{Path}/{measurementId}/deletion-impact");
        var impact = await impactResponse.Content.ReadFromJsonAsync<JsonElement>();
        using var deleteRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{Path}/{measurementId}")
        {
            Content = JsonContent.Create(new
            {
                version = impact.GetProperty("version").GetGuid(),
                impactToken = impact.GetProperty("impactToken").GetString(),
            }),
        };

        var deleteResponse = await client.SendAsync(deleteRequest);
        var history = await client.GetFromJsonAsync<JsonElement>(Path);

        Assert.Multiple(() =>
        {
            Assert.That(impactResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(impact.GetProperty("affectedSnapshotCount").GetInt32(), Is.Zero);
            Assert.That(impact.GetProperty("affectsCurrentTarget").GetBoolean(), Is.False);
            Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(history.GetProperty("totalCount").GetInt32(), Is.Zero);
        });
    }

    [Test]
    public async Task Delete_WithTamperedToken_ReturnsConflictAndKeepsRecord()
    {
        await using var factory = new BodyMeasurementApiFactory();
        using var client = CreateAuthenticatedClient(factory);
        var created = await CreateMeasurementAsync(client);
        var measurementId = created.GetProperty("measurementId").GetInt64();
        using var deleteRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{Path}/{measurementId}")
        {
            Content = JsonContent.Create(new
            {
                version = created.GetProperty("version").GetGuid(),
                impactToken = "tampered",
            }),
        };

        var response = await client.SendAsync(deleteRequest);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(json.RootElement.GetProperty("code").GetString(),
                Is.EqualTo("BodyMeasurement.Conflict"));
        });
    }

    private static async Task<JsonElement> CreateMeasurementAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(Path, new
        {
            weightInKilograms = 72m,
        });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static HttpClient CreateAuthenticatedClient(
        BodyMeasurementApiFactory factory,
        long userId = 42)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            userId.ToString());
        return client;
    }

    private sealed class BodyMeasurementApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"BodyMeasurementApiTests-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(FixedUtcNow));
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

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
