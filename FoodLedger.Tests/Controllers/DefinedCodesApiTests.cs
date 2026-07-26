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
