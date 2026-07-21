using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FoodLedger.Tests.Data;

/// <summary>
/// 驗證 <see cref="ApplicationDbContext" /> 的 EF Core 模型設定是否符合資料隔離與資料完整性需求。
/// </summary>
public class ApplicationDbContextModelTests
{
    // 測試用資料庫名稱固定值，避免 EF Core InMemory context 在測試間共用狀態。
    private const string TestDatabaseName = "ApplicationDbContextModelTests";

    /// <summary>
    /// 驗證 DailyRecord.UserId 已設定為必要欄位，避免產生沒有擁有者的飲食紀錄。
    /// </summary>
    [Test]
    public void DailyRecordUserId_WhenModelIsBuilt_IsRequired()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var entityType = GetDailyRecordEntityType(dbContext);

        // Act
        var userIdProperty = entityType.FindProperty(nameof(DailyRecord.UserId));

        // Assert
        Assert.That(userIdProperty, Is.Not.Null);
        Assert.That(userIdProperty!.IsNullable, Is.False);
    }

    /// <summary>
    /// 驗證 DailyRecord.UserId 的外鍵指向 ApplicationUser.Id，確保飲食紀錄必須屬於有效的 Identity 使用者。
    /// </summary>
    [Test]
    public void DailyRecordUserId_WhenModelIsBuilt_HasForeignKeyToApplicationUser()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var entityType = GetDailyRecordEntityType(dbContext);

        // Act
        var foreignKey = entityType.GetForeignKeys()
            .SingleOrDefault(foreignKey =>
                foreignKey.Properties.Any(property => property.Name == nameof(DailyRecord.UserId)));

        // Assert
        Assert.That(foreignKey, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(foreignKey!.PrincipalEntityType.ClrType, Is.EqualTo(typeof(ApplicationUser)));
            Assert.That(foreignKey.PrincipalKey.Properties.Single().Name, Is.EqualTo(nameof(ApplicationUser.Id)));
            Assert.That(foreignKey.DeleteBehavior, Is.EqualTo(DeleteBehavior.Restrict));
            Assert.That(foreignKey.IsRequired, Is.True);
        });
    }

    /// <summary>
    /// 驗證 DailyRecord 已建立 UserId 與 ConsumedAt 複合索引，支援後續使用者日期區間查詢。
    /// </summary>
    [Test]
    public void DailyRecord_WhenModelIsBuilt_HasUserIdConsumedAtIndex()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var entityType = GetDailyRecordEntityType(dbContext);

        // Act
        var index = entityType.GetIndexes()
            .SingleOrDefault(index =>
                index.Properties.Select(property => property.Name)
                    .SequenceEqual([nameof(DailyRecord.UserId), nameof(DailyRecord.ConsumedAt)]));

        // Assert
        Assert.That(index, Is.Not.Null);
        Assert.That(index!.GetDatabaseName(), Is.EqualTo("ix_daily_record_user_id_consumed_at"));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"{TestDatabaseName}-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IEntityType GetDailyRecordEntityType(ApplicationDbContext dbContext)
    {
        return dbContext.Model.FindEntityType(typeof(DailyRecord))
            ?? throw new InvalidOperationException("找不到 DailyRecord 的 EF Core 模型設定。");
    }
}
