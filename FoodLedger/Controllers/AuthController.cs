using FoodLedger.DTOs.Auth;
using FoodLedger.DTOs.Errors;
using FoodLedger.Infrastructure.Authentication;
using FoodLedger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodLedger.Controllers;

/// <summary>
/// 提供 FoodLedger 自訂註冊與登入 API。
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IdentityBearerTokenResponseFactory _tokenResponseFactory;

    /// <summary>
    /// 初始化 Auth Controller。
    /// </summary>
    /// <param name="authService">執行 Identity 註冊與登入流程的應用程式服務。</param>
    /// <param name="tokenResponseFactory">委派框架建立 Bearer Token 回應的 Factory。</param>
    public AuthController(
        IAuthService authService,
        IdentityBearerTokenResponseFactory tokenResponseFactory)
    {
        _authService = authService;
        _tokenResponseFactory = tokenResponseFactory;
    }

    /// <summary>
    /// 建立 FoodLedger 帳號並直接取得登入 Token。
    /// </summary>
    /// <param name="request">帳號、顯示名稱、Email 與密碼。</param>
    /// <returns>註冊成功時回傳 Token 與使用者基本資料。</returns>
    /// <remarks>
    /// 密碼由 ASP.NET Core Identity 驗證與雜湊；此 API 不要求使用者先完成 Email 驗證。
    /// </remarks>
    /// <example>
    /// <code>
    /// POST /api/auth/register
    /// {
    ///   "userAccount": "food_user",
    ///   "displayName": "Food 使用者",
    ///   "email": "user@example.com",
    ///   "password": "Password1"
    /// }
    /// </code>
    /// </example>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (result is AuthServiceSuccess success)
        {
            return Ok(await _tokenResponseFactory.CreateAsync(HttpContext, success.User));
        }

        if (result is not AuthServiceFailure failure)
        {
            throw new InvalidOperationException("Auth Service 回傳未知結果型別。");
        }
        var fieldErrors = failure.ErrorField is not null
            ? new Dictionary<string, IReadOnlyList<ApiFieldError>>
            {
                [failure.ErrorField] =
                [
                    new ApiFieldError
                    {
                        Code = failure.ErrorCode,
                        Message = failure.ErrorMessage,
                    },
                ],
            }
            : null;

        return BadRequest(new ApiErrorResponse
        {
            Code = failure.ErrorCode,
            Message = failure.ErrorMessage,
            TraceId = HttpContext.TraceIdentifier,
            Errors = fieldErrors,
        });
    }

    /// <summary>
    /// 使用帳號或 Email 與密碼登入 FoodLedger。
    /// </summary>
    /// <param name="request">帳號或 Email，以及使用者密碼。</param>
    /// <returns>登入成功時回傳 Token 與使用者基本資料；驗證失敗時回傳統一的未授權錯誤。</returns>
    /// <remarks>
    /// 不論帳號不存在或密碼錯誤，皆回傳 <c>Auth.InvalidCredentials</c>，避免透露帳號存在狀態。
    /// </remarks>
    /// <example>
    /// <code>
    /// POST /api/auth/login
    /// {
    ///   "loginId": "food_user",
    ///   "password": "Password1"
    /// }
    /// </code>
    /// </example>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result is AuthServiceSuccess success)
        {
            return Ok(await _tokenResponseFactory.CreateAsync(HttpContext, success.User));
        }

        if (result is not AuthServiceFailure failure)
        {
            throw new InvalidOperationException("Auth Service 回傳未知結果型別。");
        }
        return Unauthorized(new ApiErrorResponse
        {
            Code = failure.ErrorCode,
            Message = failure.ErrorMessage,
            TraceId = HttpContext.TraceIdentifier,
        });
    }
}
