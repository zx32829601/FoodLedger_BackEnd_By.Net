using FoodLedger.DTOs.Auth;
using FoodLedger.DTOs.Errors;
using FoodLedger.Infrastructure.Authentication;
using FoodLedger.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodLedger.Controllers;

/// <summary>
/// 提供 FoodLedger 自訂註冊與登入 API。
/// </summary>
[ApiController]
[Authorize]
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
    /// <param name="useCookies">
    /// Web client 設為 <see langword="true" /> 時建立 HttpOnly Identity Cookie；
    /// 預設回傳供行動端使用的 Bearer Token。
    /// </param>
    [HttpPost("register")]
    [AllowAnonymous]
    [CookieAntiforgery]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        [FromQuery] bool useCookies = false)
    {
        var result = await _authService.RegisterAsync(request);
        if (result is AuthServiceSuccess success)
        {
            if (useCookies)
            {
                return Ok(await _tokenResponseFactory.CreateCookieAsync(HttpContext, success.User));
            }

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
    /// <param name="useCookies">
    /// Web client 設為 <see langword="true" /> 時建立 HttpOnly Identity Cookie；
    /// 預設回傳供行動端使用的 Bearer Token。
    /// </param>
    [HttpPost("login")]
    [AllowAnonymous]
    [CookieAntiforgery]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        [FromQuery] bool useCookies = false)
    {
        var result = await _authService.LoginAsync(request);
        if (result is AuthServiceSuccess success)
        {
            if (useCookies)
            {
                return Ok(await _tokenResponseFactory.CreateCookieAsync(HttpContext, success.User));
            }

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

    /// <summary>
    /// 清除目前 Web 使用者的 Identity Cookie Session。
    /// </summary>
    /// <returns>Cookie 清除完成後回傳 <c>204 No Content</c>。</returns>
    /// <remarks>
    /// 此端點只負責 Web Cookie 登出；行動端 Bearer Token 的撤銷與 Refresh Token
    /// 管理應由後續完整 Token lifecycle 功能處理。
    /// </remarks>
    [HttpPost("logout")]
    [CookieAntiforgery]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAsync()
    {
        await HttpContext.SignOutAsync(AuthenticationSchemeNames.WebCookie);
        return NoContent();
    }

    /// <summary>
    /// 建立 Web Cookie 狀態變更 request 所需的 Antiforgery Token。
    /// </summary>
    /// <param name="antiforgery">ASP.NET Core Antiforgery Token 服務。</param>
    /// <returns>必須放入 <c>X-CSRF-TOKEN</c> Header 的 request token。</returns>
    [HttpGet("antiforgery")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AntiforgeryTokenResponse), StatusCodes.Status200OK)]
    public ActionResult<AntiforgeryTokenResponse> GetAntiforgeryToken(
        [FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new AntiforgeryTokenResponse
        {
            RequestToken = tokens.RequestToken
                ?? throw new InvalidOperationException("Antiforgery Service 未產生 Request Token。"),
        });
    }
}
