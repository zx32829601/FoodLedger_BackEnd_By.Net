using System.Text.Json;
using FoodLedger.Data.Entities;
using FoodLedger.DTOs.Auth;
using FoodLedger.DTOs.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;

namespace FoodLedger.Infrastructure.Authentication;

/// <summary>
/// 委派 ASP.NET Core Bearer Token handler 建立標準 Token，再組合 FoodLedger 使用者回應。
/// </summary>
public sealed class IdentityBearerTokenResponseFactory
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsPrincipalFactory;

    /// <summary>
    /// 初始化 Identity Bearer Token 回應 Factory。
    /// </summary>
    /// <param name="claimsPrincipalFactory">由 Identity 建立使用者 ClaimsPrincipal 的框架服務。</param>
    public IdentityBearerTokenResponseFactory(
        IUserClaimsPrincipalFactory<ApplicationUser> claimsPrincipalFactory)
    {
        _claimsPrincipalFactory = claimsPrincipalFactory;
    }

    /// <summary>
    /// 透過已註冊的 ASP.NET Core Bearer Token handler 產生 Token 回應。
    /// </summary>
    /// <param name="httpContext">目前 API request 的 HTTP context。</param>
    /// <param name="user">已由 Identity 建立或驗證的使用者。</param>
    /// <returns>包含框架 Token 與公開使用者資料的 Auth 回應。</returns>
    /// <remarks>
    /// Token protector、有效期限與序列化格式皆由框架 handler 管理；此 Factory
    /// 不實作密碼驗證、Token 保護或 Refresh Token 安全細節。
    /// </remarks>
    public async Task<AuthResponse> CreateAsync(
        HttpContext httpContext,
        ApplicationUser user)
    {
        var originalBody = httpContext.Response.Body;
        var originalContentType = httpContext.Response.ContentType;
        var originalContentLength = httpContext.Response.ContentLength;
        await using var tokenResponseBody = new MemoryStream();
        httpContext.Response.Body = tokenResponseBody;

        try
        {
            var principal = await _claimsPrincipalFactory.CreateAsync(user);
            await httpContext.SignInAsync(IdentityConstants.BearerScheme, principal);

            tokenResponseBody.Position = 0;
            var tokenResponse = await JsonSerializer.DeserializeAsync<AccessTokenResponse>(
                tokenResponseBody,
                SerializerOptions)
                ?? throw new InvalidOperationException("Bearer Token handler 未產生登入回應。");

            return new AuthResponse
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresIn = tokenResponse.ExpiresIn,
                User = CurrentUserResponseMapper.Map(user),
            };
        }
        finally
        {
            httpContext.Response.Body = originalBody;
            httpContext.Response.ContentType = originalContentType;
            httpContext.Response.ContentLength = originalContentLength;
        }
    }

    /// <summary>
    /// 建立 Web Identity Cookie，並只回傳公開使用者資料。
    /// </summary>
    /// <param name="httpContext">目前 API request 的 HTTP context。</param>
    /// <param name="user">已由 Identity 建立或驗證的使用者。</param>
    /// <returns>不暴露 Bearer Token 的 Cookie 登入回應。</returns>
    public async Task<AuthResponse> CreateCookieAsync(
        HttpContext httpContext,
        ApplicationUser user)
    {
        var principal = await _claimsPrincipalFactory.CreateAsync(user);
        await httpContext.SignInAsync(AuthenticationSchemeNames.WebCookie, principal);

        return new AuthResponse
        {
            User = CurrentUserResponseMapper.Map(user),
        };
    }
}
