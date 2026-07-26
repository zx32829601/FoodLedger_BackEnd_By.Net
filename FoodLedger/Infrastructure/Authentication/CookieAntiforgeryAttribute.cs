using Microsoft.AspNetCore.Mvc;

namespace FoodLedger.Infrastructure.Authentication;

/// <summary>
/// 要求 Cookie 模式 request 通過 Antiforgery 驗證，Bearer request 則維持 Token 驗證邊界。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CookieAntiforgeryAttribute : TypeFilterAttribute
{
    /// <summary>
    /// 建立 Cookie Antiforgery Filter。
    /// </summary>
    public CookieAntiforgeryAttribute()
        : base(typeof(CookieAntiforgeryFilter))
    {
    }
}
