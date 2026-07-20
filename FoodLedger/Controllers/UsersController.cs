using FoodLedger.DTOs.Users;
using FoodLedger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodLedger.Controllers;

/// <summary>
/// 提供使用者自身資料相關 API。
/// </summary>
/// <remarks>
/// 此 Controller 只處理目前登入使用者的 HTTP request / response，不直接查詢或暴露
/// <c>ApplicationUser</c> entity。
/// </remarks>
[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// 初始化使用者 Controller。
    /// </summary>
    /// <param name="currentUserService">
    /// 目前登入使用者服務，用來取得已通過授權 middleware 驗證的使用者資訊。
    /// </param>
    public UsersController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// 取得目前登入使用者的基本資訊。
    /// </summary>
    /// <returns>
    /// 若目前 request 可解析出登入使用者識別碼，回傳 <see cref="CurrentUserResponse" />；
    /// 若登入狀態異常或缺少使用者識別碼，回傳 <c>401 Unauthorized</c>。
    /// </returns>
    /// <remarks>
    /// 此 API 需要登入後呼叫，未帶有效 token 或 cookie 時會由授權 middleware 回傳
    /// <c>401 Unauthorized</c>。Action 內的 Unauthorized 回應用於處理已進入 action
    /// 但無法解析使用者識別碼的異常身分狀態。
    /// </remarks>
    /// <example>
    /// Request:
    /// <code>
    /// GET /api/users/me
    /// Authorization: Bearer {accessToken}
    /// </code>
    ///
    /// Response:
    /// <code>
    /// {
    ///   "userId": 42,
    ///   "userName": "food-ledger-user",
    ///   "isAuthenticated": true
    /// }
    /// </code>
    /// </example>
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetMe()
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Unauthorized();
        }

        var response = new CurrentUserResponse
        {
            UserId = userId,
            UserName = _currentUserService.UserName,
            IsAuthenticated = _currentUserService.IsAuthenticated,
        };

        return Ok(response);
    }
}
