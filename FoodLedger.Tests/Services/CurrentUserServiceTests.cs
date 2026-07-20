using System.Security.Claims;
using FoodLedger.Services;
using Microsoft.AspNetCore.Http;

namespace FoodLedger.Tests.Services;

/// <summary>
/// 驗證 <see cref="CurrentUserService" /> 從 HTTP context 解析目前登入使用者資訊的行為。
/// </summary>
public class CurrentUserServiceTests
{
    // 測試用固定值，用來確認 claim 可正確轉換成系統使用者識別碼。
    private const long AuthenticatedUserId = 42;

    // 測試用固定值，用來確認目前使用者名稱可從 ClaimsIdentity 取出。
    private const string AuthenticatedUserName = "food-ledger-user";

    /// <summary>
    /// 驗證已登入使用者具備合法 NameIdentifier claim 時，會回傳對應的使用者識別碼。
    /// </summary>
    [Test]
    public void UserId_WhenUserIsAuthenticated_ReturnsNameIdentifierClaimValue()
    {
        // 準備
        var httpContextAccessor = CreateHttpContextAccessor(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, AuthenticatedUserId.ToString())],
                    authenticationType: "Test")));
        var service = new CurrentUserService(httpContextAccessor);

        // 執行
        var userId = service.UserId;

        // 驗證
        Assert.That(userId, Is.EqualTo(AuthenticatedUserId));
    }

    /// <summary>
    /// 驗證未登入使用者沒有可用身份資訊時，使用者識別碼會回傳 null。
    /// </summary>
    [Test]
    public void UserId_WhenUserIsNotAuthenticated_ReturnsNull()
    {
        // 準備
        var httpContextAccessor = CreateHttpContextAccessor(new ClaimsPrincipal(new ClaimsIdentity()));
        var service = new CurrentUserService(httpContextAccessor);

        // 執行
        var userId = service.UserId;

        // 驗證
        Assert.That(userId, Is.Null);
    }

    /// <summary>
    /// 驗證未通過驗證的 identity 即使帶有使用者識別碼 claim，也不會被視為可信身份。
    /// </summary>
    [Test]
    public void UserId_WhenUserHasClaimButIsNotAuthenticated_ReturnsNull()
    {
        // 準備
        var httpContextAccessor = CreateHttpContextAccessor(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, AuthenticatedUserId.ToString())])));
        var service = new CurrentUserService(httpContextAccessor);

        // 執行
        var userId = service.UserId;

        // 驗證
        Assert.That(userId, Is.Null);
    }

    /// <summary>
    /// 驗證已登入但缺少 NameIdentifier claim 時，使用者識別碼會回傳 null。
    /// </summary>
    [Test]
    public void UserId_WhenNameIdentifierClaimIsMissing_ReturnsNull()
    {
        // 準備
        var httpContextAccessor = CreateHttpContextAccessor(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, AuthenticatedUserName)],
                    authenticationType: "Test")));
        var service = new CurrentUserService(httpContextAccessor);

        // 執行
        var userId = service.UserId;

        // 驗證
        Assert.That(userId, Is.Null);
    }

    /// <summary>
    /// 驗證 NameIdentifier claim 無法轉成 long 時，使用者識別碼會回傳 null 並避免拋出例外。
    /// </summary>
    [Test]
    public void UserId_WhenNameIdentifierClaimIsNotLong_ReturnsNull()
    {
        // 準備
        var httpContextAccessor = CreateHttpContextAccessor(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "invalid-user-id")],
                    authenticationType: "Test")));
        var service = new CurrentUserService(httpContextAccessor);

        // 執行
        var userId = service.UserId;

        // 驗證
        Assert.That(userId, Is.Null);
    }

    /// <summary>
    /// 驗證目前 identity 已通過驗證時，登入狀態會回傳 true。
    /// </summary>
    [Test]
    public void IsAuthenticated_WhenUserIsAuthenticated_ReturnsTrue()
    {
        // 準備
        var httpContextAccessor = CreateHttpContextAccessor(
            new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "Test")));
        var service = new CurrentUserService(httpContextAccessor);

        // 執行
        var isAuthenticated = service.IsAuthenticated;

        // 驗證
        Assert.That(isAuthenticated, Is.True);
    }

    /// <summary>
    /// 驗證沒有 HTTP context 的非 request 情境會被視為未登入，且不會拋出例外。
    /// </summary>
    [Test]
    public void IsAuthenticated_WhenHttpContextIsNull_ReturnsFalse()
    {
        // 準備
        var service = new CurrentUserService(new HttpContextAccessor());

        // 執行
        var isAuthenticated = service.IsAuthenticated;

        // 驗證
        Assert.That(isAuthenticated, Is.False);
    }

    /// <summary>
    /// 驗證已登入使用者具備名稱 claim 時，會回傳目前使用者名稱。
    /// </summary>
    [Test]
    public void UserName_WhenUserHasName_ReturnsIdentityName()
    {
        // 準備
        var httpContextAccessor = CreateHttpContextAccessor(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, AuthenticatedUserName)],
                    authenticationType: "Test")));
        var service = new CurrentUserService(httpContextAccessor);

        // 執行
        var userName = service.UserName;

        // 驗證
        Assert.That(userName, Is.EqualTo(AuthenticatedUserName));
    }

    /// <summary>
    /// 驗證未通過驗證的 identity 即使帶有名稱 claim，使用者名稱仍會回傳 null。
    /// </summary>
    [Test]
    public void UserName_WhenUserIsNotAuthenticated_ReturnsNull()
    {
        // 準備
        var httpContextAccessor = CreateHttpContextAccessor(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, AuthenticatedUserName)])));
        var service = new CurrentUserService(httpContextAccessor);

        // 執行
        var userName = service.UserName;

        // 驗證
        Assert.That(userName, Is.Null);
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(ClaimsPrincipal user)
    {
        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = user,
            },
        };
    }
}
