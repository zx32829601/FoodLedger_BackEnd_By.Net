using FoodLedger.Data.Entities;
using FoodLedger.Services;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Tests.Data;

/// <summary>
/// 驗證 DbContext 以目前登入者統一維護資料稽核欄位。
/// </summary>
[TestFixture]
public sealed class ApplicationDbContextAuditTests
{
    /// <summary>
    /// 驗證新增與修改資料時分別寫入目前使用者名稱，且修改不覆蓋建立者。
    /// </summary>
    [Test]
    public async Task SaveChangesAsync_WhenUserIsAuthenticated_WritesUserNameToAuditFields()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var currentUser = new TestCurrentUserService { UserName = "food-admin" };
        await using var dbContext = new ApplicationDbContext(options, currentUser);
        var food = new SimpleFood { FoodCode = "AUDIT_TEST" };

        // Act
        dbContext.SimpleFoods.Add(food);
        await dbContext.SaveChangesAsync();
        currentUser.UserName = "food-editor";
        food.FoodCode = "AUDIT_TEST_UPDATED";
        await dbContext.SaveChangesAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(food.CreatedBy, Is.EqualTo("food-admin"));
            Assert.That(food.ModifiedBy, Is.EqualTo("food-editor"));
        });
    }

    /// <summary>
    /// 驗證無法取得目前使用者名稱時，以 System 寫入建立者與修改者。
    /// </summary>
    [Test]
    public async Task SaveChangesAsync_WhenCurrentUserNameIsUnavailable_UsesSystemActor()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new ApplicationDbContext(options);
        var food = new SimpleFood { FoodCode = "SYSTEM_AUDIT_TEST" };

        // Act
        dbContext.SimpleFoods.Add(food);
        await dbContext.SaveChangesAsync();
        food.FoodCode = "SYSTEM_AUDIT_TEST_UPDATED";
        await dbContext.SaveChangesAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(food.CreatedBy, Is.EqualTo("System"));
            Assert.That(food.ModifiedBy, Is.EqualTo("System"));
        });
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public bool IsAuthenticated => true;

        public long? UserId => 1;

        public string? UserName { get; set; }
    }
}
