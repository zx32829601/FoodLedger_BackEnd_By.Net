using Microsoft.AspNetCore.Authorization;

namespace FoodLedger.Security;

/// <summary>
/// 提供 FoodLedger 授權規則的 DI 註冊擴充方法。
/// </summary>
public static class AuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// 註冊應用程式共用授權 policy。
    /// </summary>
    /// <param name="services">要加入授權設定的服務集合。</param>
    /// <returns>原始服務集合，方便串接其他 DI 註冊。</returns>
    public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicyNames.AdminOnly,
                policy => policy.RequireRole(ApplicationRoles.Admin));
        });

        return services;
    }
}
