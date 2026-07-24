using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using FoodLedger.Data.Entities;
using FoodLedger.DTOs.DailyRecords;
using FoodLedger.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodLedger.Tests.Controllers;

/// <summary>
/// 驗證每日飲食紀錄 API 經過 ASP.NET Core middleware 後的授權行為。
/// </summary>
public class DailyRecordsApiAuthorizationTests
{
    // 測試用驗證 scheme，僅在 WebApplicationFactory 內註冊，避免依賴真實 JWT / Identity token。
    private const string TestAuthenticationScheme = "Test";

    /// <summary>
    /// 驗證未登入 request 呼叫新增每日飲食紀錄 API 時，會被 Authorize middleware 擋下並回傳 401 Unauthorized。
    /// </summary>
    [Test]
    public async Task Create_WhenRequestIsAnonymous_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new DailyRecordsApiFactory();
        using var client = factory.CreateClient();
        var dailyRecordService = factory.Services.GetRequiredService<RecordingDailyRecordService>();
        var request = new
        {
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = DateTimeOffset.UtcNow,
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/daily-records", request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(dailyRecordService.WasCalled, Is.False);
        });
    }

    /// <summary>
    /// 驗證未登入 request 呼叫查詢每日飲食紀錄 API 時，會被 Authorize middleware 擋下並回傳 401 Unauthorized。
    /// </summary>
    [Test]
    public async Task GetDailyRecords_WhenRequestIsAnonymous_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new DailyRecordsApiFactory();
        using var client = factory.CreateClient();
        var dailyRecordService = factory.Services.GetRequiredService<RecordingDailyRecordService>();

        // Act
        var response = await client.GetAsync("/api/daily-records?date=2026-07-23");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(dailyRecordService.WasCalled, Is.False);
        });
    }

    /// <summary>
    /// 驗證已通過驗證的 request 呼叫查詢每日飲食紀錄 API 時，會進入 Service 並回傳 200 OK。
    /// </summary>
    [Test]
    public async Task GetDailyRecords_WhenRequestIsAuthenticated_CallsServiceAndReturnsOk()
    {
        // Arrange
        await using var factory = new DailyRecordsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");
        var dailyRecordService = factory.Services.GetRequiredService<RecordingDailyRecordService>();
        dailyRecordService.RecordsToReturn =
        [
            new DailyRecordResponse
            {
                RecordId = 1,
                FoodId = 2,
                Quantity = 1.5m,
                ConsumedAt = new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero),
            },
        ];

        // Act
        var response = await client.GetAsync("/api/daily-records?date=2026-07-23");

        // Assert
        var records = await response.Content.ReadFromJsonAsync<DailyRecordResponse[]>();
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(dailyRecordService.GetCallCount, Is.EqualTo(1));
            Assert.That(dailyRecordService.ReceivedDate, Is.EqualTo(new DateOnly(2026, 7, 23)));
            Assert.That(records, Is.Not.Null);
            Assert.That(records!, Has.Length.EqualTo(1));
            Assert.That(records![0].RecordId, Is.EqualTo(1));
            Assert.That(records[0].FoodId, Is.EqualTo(2));
            Assert.That(records[0].Quantity, Is.EqualTo(1.5m));
        });
    }

    /// <summary>
    /// 驗證已驗證 request 的查詢日期格式無效時，API 會由模型綁定回傳 400 且不進入 Service。
    /// </summary>
    [Test]
    public async Task GetDailyRecords_WhenDateQueryIsInvalid_ReturnsBadRequestAndDoesNotCallService()
    {
        // Arrange
        await using var factory = new DailyRecordsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");
        var dailyRecordService = factory.Services.GetRequiredService<RecordingDailyRecordService>();

        // Act
        var response = await client.GetAsync("/api/daily-records?date=not-a-date");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(dailyRecordService.WasCalled, Is.False);
        });
    }

    /// <summary>
    /// 驗證已通過驗證的 request 呼叫新增每日飲食紀錄 API 時，會進入 Service 並回傳 204 No Content。
    /// </summary>
    [Test]
    public async Task Create_WhenRequestIsAuthenticated_CallsServiceAndReturnsNoContent()
    {
        // Arrange
        await using var factory = new DailyRecordsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");
        var dailyRecordService = factory.Services.GetRequiredService<RecordingDailyRecordService>();
        var consumedAt = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);
        var request = new
        {
            FoodId = 1,
            Quantity = 1.5m,
            ConsumedAt = consumedAt,
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/daily-records", request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(dailyRecordService.CallCount, Is.EqualTo(1));
            Assert.That(dailyRecordService.ReceivedRequest?.FoodId, Is.EqualTo(request.FoodId));
            Assert.That(dailyRecordService.ReceivedRequest?.Quantity, Is.EqualTo(request.Quantity));
            Assert.That(dailyRecordService.ReceivedRequest?.ConsumedAt, Is.EqualTo(consumedAt));
        });
    }

    /// <summary>
    /// 驗證已驗證 request 的食物識別碼為 0 時，API 會由模型驗證回傳 400 且不進入 Service。
    /// </summary>
    [Test]
    public async Task Create_WhenFoodIdIsZero_ReturnsBadRequestAndDoesNotCallService()
    {
        // Arrange
        await using var factory = new DailyRecordsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");
        var dailyRecordService = factory.Services.GetRequiredService<RecordingDailyRecordService>();
        var request = new
        {
            FoodId = 0,
            Quantity = 1,
            ConsumedAt = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero),
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/daily-records", request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(dailyRecordService.WasCalled, Is.False);
        });
    }

    /// <summary>
    /// 驗證已驗證 request 的食用數量為 0 時，API 會由模型驗證回傳 400 且不進入 Service。
    /// </summary>
    [Test]
    public async Task Create_WhenQuantityIsZero_ReturnsBadRequestAndDoesNotCallService()
    {
        // Arrange
        await using var factory = new DailyRecordsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");
        var dailyRecordService = factory.Services.GetRequiredService<RecordingDailyRecordService>();
        var request = new
        {
            FoodId = 1,
            Quantity = 0,
            ConsumedAt = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero),
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/daily-records", request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(dailyRecordService.WasCalled, Is.False);
        });
    }

    /// <summary>
    /// 驗證食用數量為 0 的驗證錯誤內容會包含 Quantity 欄位，讓呼叫端可對應欄位錯誤。
    /// </summary>
    [Test]
    public async Task Create_WhenQuantityIsZero_ReturnsValidationProblemWithQuantityError()
    {
        // Arrange
        await using var factory = new DailyRecordsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");
        var dailyRecordService = factory.Services.GetRequiredService<RecordingDailyRecordService>();
        var request = new
        {
            FoodId = 1,
            Quantity = 0,
            ConsumedAt = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero),
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/daily-records", request);

        // Assert
        await using var responseStream = await response.Content.ReadAsStreamAsync();
        using var problemDetails = await JsonDocument.ParseAsync(responseStream);
        var errors = problemDetails.RootElement.GetProperty("errors");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(errors.TryGetProperty(nameof(CreateDailyRecordRequest.Quantity), out _), Is.True);
            Assert.That(dailyRecordService.WasCalled, Is.False);
        });
    }

    /// <summary>
    /// 驗證食用數量為 0 的 Quantity 欄位錯誤會以非空陣列回傳，讓呼叫端可安全渲染錯誤清單。
    /// </summary>
    [Test]
    public async Task Create_WhenQuantityIsZero_ReturnsNonEmptyQuantityErrorsArray()
    {
        // Arrange
        await using var factory = new DailyRecordsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");
        var dailyRecordService = factory.Services.GetRequiredService<RecordingDailyRecordService>();
        var request = new
        {
            FoodId = 1,
            Quantity = 0,
            ConsumedAt = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero),
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/daily-records", request);

        // Assert
        await using var responseStream = await response.Content.ReadAsStreamAsync();
        using var problemDetails = await JsonDocument.ParseAsync(responseStream);
        var quantityErrors = problemDetails.RootElement
            .GetProperty("errors")
            .GetProperty(nameof(CreateDailyRecordRequest.Quantity));

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(quantityErrors.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(quantityErrors.GetArrayLength(), Is.GreaterThan(0));
            Assert.That(dailyRecordService.WasCalled, Is.False);
        });
    }

    /// <summary>
    /// 驗證已驗證 request 的食用數量為負數時，API 會由模型驗證回傳 400 且不進入 Service。
    /// </summary>
    [Test]
    public async Task Create_WhenQuantityIsNegative_ReturnsBadRequestAndDoesNotCallService()
    {
        // Arrange
        await using var factory = new DailyRecordsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");
        var dailyRecordService = factory.Services.GetRequiredService<RecordingDailyRecordService>();
        var request = new
        {
            FoodId = 1,
            Quantity = -1,
            ConsumedAt = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero),
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/daily-records", request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(dailyRecordService.WasCalled, Is.False);
        });
    }

    /// <summary>
    /// 驗證已驗證 request 的食用數量超過 API 可接受上限時，API 會由模型驗證回傳 400 且不進入 Service。
    /// </summary>
    [Test]
    public async Task Create_WhenQuantityExceedsMaximum_ReturnsBadRequestAndDoesNotCallService()
    {
        // Arrange
        await using var factory = new DailyRecordsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");
        var dailyRecordService = factory.Services.GetRequiredService<RecordingDailyRecordService>();
        var request = new
        {
            FoodId = 1,
            Quantity = 10000000m,
            ConsumedAt = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero),
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/daily-records", request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(dailyRecordService.WasCalled, Is.False);
        });
    }

    /// <summary>
    /// 驗證已驗證 request 的食用數量超過業務上限時，API 會由模型驗證回傳 400 且不進入 Service。
    /// </summary>
    [Test]
    public async Task Create_WhenQuantityExceedsBusinessMaximum_ReturnsBadRequestAndDoesNotCallService()
    {
        // Arrange
        await using var factory = new DailyRecordsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");
        var dailyRecordService = factory.Services.GetRequiredService<RecordingDailyRecordService>();
        var request = new
        {
            FoodId = 1,
            Quantity = 10000.001m,
            ConsumedAt = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero),
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/daily-records", request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(dailyRecordService.WasCalled, Is.False);
        });
    }

    /// <summary>
    /// 驗證每日飲食紀錄 API 收到最大合法業務食用量時，應通過模型驗證並交由 Service 處理。
    /// </summary>
    [Test]
    public async Task Create_WhenQuantityIsMaximumAllowedValue_CallsServiceAndReturnsNoContent()
    {
        // Arrange
        await using var factory = new DailyRecordsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationScheme,
            "authenticated");
        var dailyRecordService = factory.Services.GetRequiredService<RecordingDailyRecordService>();
        var consumedAt = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);
        var request = new
        {
            FoodId = 1,
            Quantity = 10000m,
            ConsumedAt = consumedAt,
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/daily-records", request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(dailyRecordService.CallCount, Is.EqualTo(1));
            Assert.That(dailyRecordService.ReceivedRequest?.Quantity, Is.EqualTo(request.Quantity));
        });
    }

    private sealed class DailyRecordsApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDailyRecordService>();
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
                    options.UseInMemoryDatabase($"DailyRecordsApiAuthorizationTests-{Guid.NewGuid()}"));
                services.AddSingleton<RecordingDailyRecordService>();
                services.AddSingleton<IDailyRecordService>(serviceProvider =>
                    serviceProvider.GetRequiredService<RecordingDailyRecordService>());
            });
        }
    }

    private sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.Authorization.Any(header =>
                    AuthenticationHeaderValue.TryParse(header, out var authenticationHeader)
                    && authenticationHeader.Scheme == TestAuthenticationScheme))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(ClaimTypes.Name, "food-ledger-test-user"),
            };
            var identity = new ClaimsIdentity(claims, TestAuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, TestAuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class RecordingDailyRecordService : IDailyRecordService
    {
        public int CallCount { get; private set; }

        public int GetCallCount { get; private set; }

        public bool WasCalled { get; private set; }

        public CreateDailyRecordRequest? ReceivedRequest { get; private set; }

        public DateOnly? ReceivedDate { get; private set; }

        public IReadOnlyList<DailyRecordResponse> RecordsToReturn { get; set; } = [];

        public Task CreateDailyRecordAsync(
            CreateDailyRecordRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            WasCalled = true;
            ReceivedRequest = request;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DailyRecordResponse>> GetDailyRecordsAsync(
            DateOnly date,
            CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            WasCalled = true;
            ReceivedDate = date;
            return Task.FromResult(RecordsToReturn);
        }
    }
}
