using FoodLedger.Controllers;
using FoodLedger.Infrastructure.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;

namespace FoodLedger.Tests.Controllers;

/// <summary>
/// 驗證 API Controller 的授權邊界是否明確標示，避免新增匿名端點時沒有被注意到。
/// </summary>
public class ControllerAuthorizationPolicyTests
{
    /// <summary>
    /// 驗證每個 API Controller 都必須明確標示授權、匿名或 Development-only 用途。
    /// </summary>
    [Test]
    public void ApiControllers_WhenDiscovered_HaveExplicitAuthorizationBoundary()
    {
        // Arrange
        var controllerTypes = typeof(UsersController).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsClass: true })
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
            .ToArray();

        // Act
        var controllersWithoutBoundary = controllerTypes
            .Where(type => !HasAuthorizationBoundary(type))
            .Select(type => type.Name)
            .ToArray();

        // Assert
        Assert.That(controllersWithoutBoundary, Is.Empty);
    }

    /// <summary>
    /// 驗證資料庫連線測試 Controller 明確標示為 Development-only，避免被誤認為正式業務 API。
    /// </summary>
    [Test]
    public void TestDbController_WhenDeclared_IsDevelopmentOnly()
    {
        // Arrange
        var controllerType = typeof(TestDbController);

        // Act
        var developmentOnlyAttribute = controllerType
            .GetCustomAttributes(typeof(DevelopmentOnlyControllerAttribute), inherit: false)
            .SingleOrDefault();

        // Assert
        Assert.That(developmentOnlyAttribute, Is.Not.Null);
    }

    /// <summary>
    /// 驗證非 Development 環境會排除 Development-only Controller，避免診斷端點出現在正式環境。
    /// </summary>
    [Test]
    public void PopulateFeature_WhenEnvironmentIsNotDevelopment_RemovesDevelopmentOnlyController()
    {
        // Arrange
        var provider = new DevelopmentOnlyControllerFeatureProvider(isDevelopment: false);
        var feature = new ControllerFeature();
        feature.Controllers.Add(typeof(TestDbController).GetTypeInfo());
        feature.Controllers.Add(typeof(UsersController).GetTypeInfo());

        // Act
        provider.PopulateFeature(Array.Empty<ApplicationPart>(), feature);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(feature.Controllers, Does.Not.Contain(typeof(TestDbController).GetTypeInfo()));
            Assert.That(feature.Controllers, Does.Contain(typeof(UsersController).GetTypeInfo()));
        });
    }

    /// <summary>
    /// 驗證 Development 環境會保留 Development-only Controller，方便本機手動診斷。
    /// </summary>
    [Test]
    public void PopulateFeature_WhenEnvironmentIsDevelopment_KeepsDevelopmentOnlyController()
    {
        // Arrange
        var provider = new DevelopmentOnlyControllerFeatureProvider(isDevelopment: true);
        var feature = new ControllerFeature();
        feature.Controllers.Add(typeof(TestDbController).GetTypeInfo());

        // Act
        provider.PopulateFeature(Array.Empty<ApplicationPart>(), feature);

        // Assert
        Assert.That(feature.Controllers, Does.Contain(typeof(TestDbController).GetTypeInfo()));
    }

    private static bool HasAuthorizationBoundary(Type controllerType)
    {
        return controllerType.IsDefined(typeof(AuthorizeAttribute), inherit: false)
            || controllerType.IsDefined(typeof(AllowAnonymousAttribute), inherit: false)
            || controllerType.IsDefined(typeof(DevelopmentOnlyControllerAttribute), inherit: false);
    }
}
