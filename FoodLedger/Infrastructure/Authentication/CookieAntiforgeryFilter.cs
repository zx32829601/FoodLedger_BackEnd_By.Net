using FoodLedger.DTOs.Errors;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodLedger.Infrastructure.Authentication;

/// <summary>
/// 驗證瀏覽器自動攜帶 Cookie 的狀態變更 request，降低 CSRF 風險。
/// </summary>
public sealed class CookieAntiforgeryFilter : IAsyncAuthorizationFilter
{
    private const string InvalidAntiforgeryTokenCode = "Security.InvalidAntiforgeryToken";

    private readonly IAntiforgery _antiforgery;

    /// <summary>
    /// 建立 Cookie Antiforgery Filter。
    /// </summary>
    /// <param name="antiforgery">ASP.NET Core Antiforgery 驗證服務。</param>
    public CookieAntiforgeryFilter(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    /// <inheritdoc />
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        if (!string.IsNullOrWhiteSpace(request.Headers.Authorization))
        {
            return;
        }

        var isAnonymousEndpoint =
            context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        var requestsCookieMode = request.Query.TryGetValue("useCookies", out var useCookies)
            && string.Equals(useCookies, bool.TrueString, StringComparison.OrdinalIgnoreCase);
        if (isAnonymousEndpoint && !requestsCookieMode)
        {
            return;
        }

        try
        {
            await _antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new BadRequestObjectResult(new ApiErrorResponse
            {
                Code = InvalidAntiforgeryTokenCode,
                Message = "無法驗證此請求，請重新整理頁面後再試。",
                TraceId = context.HttpContext.TraceIdentifier,
            });
        }
    }
}
