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
            CreateCode("MealType", "Dinner", "晚餐", 3, isActive: true),
            CreateCode("MealType", "Breakfast", "早餐", 1, isActive: true),
            CreateCode("MealType", "Lunch", "午餐", 2, isActive: false),
            CreateCode("AccountStatus", "Active", "啟用", 0, isActive: true));
        await dbContext.SaveChangesAsync();
        var service = new DefinedCodeService(dbContext);

        // Act
        var result = await service.GetActiveMealTypesAsync("zh-TW");

        // Assert
        Assert.That(result.Select(code => code.Code), Is.EqualTo(new[] { "Breakfast", "Dinner" }));
        Assert.That(result.Select(code => code.DisplayName), Is.EqualTo(new[] { "早餐", "晚餐" }));
        Assert.That(result.Select(code => code.SortOrder), Is.EqualTo(new[] { 1, 3 }));
    }

    private static DefinedCode CreateCode(
        string codeType,
        string code,
        string displayName,
        int sortOrder,
        bool isActive)
    {
        return new DefinedCode
        {
            CodeType = codeType,
            Code = code,
            SortOrder = sortOrder,
            IsActive = isActive,
            Translations =
            [
                new DefinedCodeTranslation
                {
                    CodeType = codeType,
                    Code = code,
                    LangCode = "zh-TW",
                    DisplayName = displayName,
                    Note = $"{displayName}說明",
                },
            ],
        };
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"DefinedCodeServiceTests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
