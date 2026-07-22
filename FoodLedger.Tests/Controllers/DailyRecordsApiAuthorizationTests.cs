using System.Net;
using System.Net.Http.Json;
using FoodLedger.Data.Entities;
using FoodLedger.DTOs.DailyRecords;
using FoodLedger.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodLedger.Tests.Controllers;

/// <summary>
/// 驗證每日飲食紀錄 API 經過 ASP.NET Core middleware 後的授權行為。
/// </summary>
public class DailyRecordsApiAuthorizationTests
{
    /// <summary>
    /// 驗證未登入 request 呼叫新增每日飲食紀錄 API 時，會被 Authorize middleware 擋下並回傳 401 Unauthorized。
    /// </summary>
    [Test]
    public async Task Create_WhenRequestIsAnonymous_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new DailyRecordsApiFactory();
        using var client = factory.CreateClient();
        var dailyRecordService = factory.Services.GetRequiredService<BlockingDailyRecordService>();
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

    private sealed class DailyRecordsApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDailyRecordService>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase($"DailyRecordsApiAuthorizationTests-{Guid.NewGuid()}"));
                services.AddSingleton<BlockingDailyRecordService>();
                services.AddSingleton<IDailyRecordService>(serviceProvider =>
                    serviceProvider.GetRequiredService<BlockingDailyRecordService>());
            });
        }
    }

    private sealed class BlockingDailyRecordService : IDailyRecordService
    {
        public bool WasCalled { get; private set; }

        public Task CreateDailyRecordAsync(
            CreateDailyRecordRequest request,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            throw new InvalidOperationException("匿名 request 不應進入每日飲食紀錄 Service。");
        }
    }
}
