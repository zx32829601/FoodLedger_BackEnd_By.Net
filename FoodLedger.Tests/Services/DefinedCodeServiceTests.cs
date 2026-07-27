using FoodLedger.Data.Entities;
using FoodLedger.Services;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Tests.Services;

/// <summary>
/// 驗證 <see cref="DefinedCodeService" /> 的通用代碼查詢規則。
/// </summary>
public class DefinedCodeServiceTests
{
    /// <summary>
    /// 驗證查詢餐別時，只回傳啟用的 MealType，並依顯示順序排列。
    /// </summary>
    [Test]
    public async Task GetActiveMealTypesAsync_WhenCodesExist_ReturnsActiveMealTypesOrderedBySortOrder()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.DefinedCodes.AddRange(
            new DefinedCode
            {
                CodeType = "MealType",
                Code = "Dinner",
                DisplayName = "晚餐",
                SortOrder = 3,
                IsActive = true,
            },
            new DefinedCode
            {
                CodeType = "MealType",
                Code = "Breakfast",
                DisplayName = "早餐",
                SortOrder = 1,
                IsActive = true,
            },
            new DefinedCode
            {
                CodeType = "MealType",
                Code = "Lunch",
                DisplayName = "午餐",
                SortOrder = 2,
                IsActive = false,
            },
            new DefinedCode
            {
                CodeType = "AccountStatus",
                Code = "Active",
                DisplayName = "啟用",
                SortOrder = 0,
                IsActive = true,
            });
        await dbContext.SaveChangesAsync();
        var service = new DefinedCodeService(dbContext);

        // Act
        var result = await service.GetActiveMealTypesAsync();

        // Assert
        Assert.That(result.Select(code => code.Code), Is.EqualTo(new[] { "Breakfast", "Dinner" }));
        Assert.That(result.Select(code => code.DisplayName), Is.EqualTo(new[] { "早餐", "晚餐" }));
        Assert.That(result.Select(code => code.SortOrder), Is.EqualTo(new[] { 1, 3 }));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"DefinedCodeServiceTests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
