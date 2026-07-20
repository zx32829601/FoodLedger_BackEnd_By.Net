using System.Security.Claims;

namespace FoodLedger.Services;

/// <summary>
/// 從目前 HTTP 請求解析登入使用者資訊的服務。
/// </summary>
/// <remarks>
/// 此類別是 Web API 與 Service 層之間的薄封裝。業務 Service 應依賴
/// <see cref="ICurrentUserService" />，避免直接依賴 <c>IHttpContextAccessor</c>。
/// </remarks>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 初始化目前使用者服務。
    /// </summary>
    /// <param name="httpContextAccessor">
    /// ASP.NET Core 提供的 HTTP context 存取器，用來讀取目前請求的
    /// <see cref="ClaimsPrincipal" />。
    /// </param>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public long? UserId
    {
        get
        {
            if (!IsAuthenticated)
            {
                return null;
            }

            var userIdValue = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            return long.TryParse(userIdValue, out var userId)
                ? userId
                : null;
        }
    }

    /// <inheritdoc />
    public string? UserName =>
        IsAuthenticated
            ? _httpContextAccessor.HttpContext?.User.Identity?.Name
            : null;
}
