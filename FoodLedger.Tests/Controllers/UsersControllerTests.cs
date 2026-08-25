using FoodLedger.Controllers;
using FoodLedger.DTOs.Auth;
using FoodLedger.DTOs.Users;
using FoodLedger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodLedger.Tests.Controllers;

/// <summary>
/// 驗證 <see cref="UsersController" /> 對目前登入使用者資訊 API 的 HTTP 行為與路由設定。
/// </summary>
[Category("Users")]
[Category("Unit")]
public class UsersControllerTests
{
    // 測試用固定值，用來代表目前登入使用者的系統識別碼。
    private const long CurrentUserId = 42;

    // 測試用固定值，用來代表目前登入使用者帳號。
    private const string CurrentUserAccount = "food-ledger-user";

    // 測試用固定值，用來代表目前登入使用者顯示名稱。
    private const string CurrentDisplayName = "Food 使用者";

    // 測試用固定值，用來代表目前登入使用者 Email。
    private const string CurrentEmail = "user@example.com";

    /// <summary>
    /// 驗證使用者資訊 Controller 必須套用 Authorize attribute，避免匿名呼叫取得目前使用者資訊。
    /// </summary>
    [Test]
    public void UsersController_HasAuthorizeAttribute()
    {
        // 準備
        var controllerType = typeof(UsersController);

        // 執行
        var authorizeAttribute = controllerType
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .SingleOrDefault();

        // 驗證
        Assert.That(authorizeAttribute, Is.Not.Null);
    }

    /// <summary>
    /// 驗證使用者資訊 Controller 的基底路由為 api/users，符合公開 API 路由規劃。
    /// </summary>
    [Test]
    public void UsersController_HasExpectedRouteAttribute()
    {
        // 準備
        var controllerType = typeof(UsersController);

        // 執行
        var routeAttribute = controllerType
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .SingleOrDefault();

        // 驗證
        Assert.That(routeAttribute?.Template, Is.EqualTo("api/users"));
    }

    /// <summary>
    /// 驗證取得目前登入者資訊的 action 使用 GET me 路由，組合後為 GET /api/users/me。
    /// </summary>
    [Test]
    public void GetMeAsync_HasExpectedHttpGetAttribute()
    {
        // 準備
        var methodInfo = typeof(UsersController).GetMethod(nameof(UsersController.GetMeAsync));

        // 執行
        var httpGetAttribute = methodInfo?
            .GetCustomAttributes(typeof(HttpGetAttribute), inherit: false)
            .Cast<HttpGetAttribute>()
            .SingleOrDefault();

        // 驗證
        Assert.That(httpGetAttribute?.Template, Is.EqualTo("me"));
    }

    /// <summary>
    /// 驗證目前使用者識別碼存在時，GetMe 會回傳 200 OK 與目前使用者 DTO。
    /// </summary>
    [Test]
    public async Task GetMe_WhenCurrentUserExists_ReturnsCurrentUserResponse()
    {
        // 準備
        var currentUserService = new TestCurrentUserService
        {
            IsAuthenticated = true,
            UserId = CurrentUserId,
            UserName = CurrentUserAccount,
        };
        var authService = new TestAuthService
        {
            CurrentUserToReturn = new CurrentUserResponse
            {
                UserId = CurrentUserId,
                UserAccount = CurrentUserAccount,
                DisplayName = CurrentDisplayName,
                Email = CurrentEmail,
            },
        };
        var controller = new UsersController(currentUserService, authService);

        // 執行
        var result = await controller.GetMeAsync();

        // 驗證
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);

        var response = okResult!.Value as CurrentUserResponse;
        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.UserId, Is.EqualTo(CurrentUserId));
            Assert.That(response.UserAccount, Is.EqualTo(CurrentUserAccount));
            Assert.That(response.DisplayName, Is.EqualTo(CurrentDisplayName));
            Assert.That(response.Email, Is.EqualTo(CurrentEmail));
        });
    }

    /// <summary>
    /// 驗證目前使用者識別碼缺失時，GetMe 會回傳 401 Unauthorized。
    /// </summary>
    [Test]
    public async Task GetMe_WhenCurrentUserIdIsMissing_ReturnsUnauthorized()
    {
        // 準備
        var currentUserService = new TestCurrentUserService
        {
            IsAuthenticated = false,
            UserId = null,
            UserName = null,
        };
        var controller = new UsersController(currentUserService, new TestAuthService());

        // 執行
        var result = await controller.GetMeAsync();

        // 驗證
        Assert.That(result, Is.TypeOf<UnauthorizedResult>());
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public bool IsAuthenticated { get; init; }

        public long? UserId { get; init; }

        public string? UserName { get; init; }
    }

    private sealed class TestAuthService : IAuthService
    {
        public CurrentUserResponse? CurrentUserToReturn { get; init; }

        public Task<AuthServiceResult> RegisterAsync(RegisterRequest request)
        {
            throw new NotSupportedException();
        }

        public Task<AuthServiceResult> LoginAsync(LoginRequest request)
        {
            throw new NotSupportedException();
        }

        public Task<CurrentUserResponse?> GetCurrentUserAsync(long userId)
        {
            return Task.FromResult(CurrentUserToReturn);
        }
    }
}
