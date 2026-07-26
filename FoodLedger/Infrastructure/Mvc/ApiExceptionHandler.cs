using FoodLedger.DTOs.Errors;
using Microsoft.AspNetCore.Diagnostics;

namespace FoodLedger.Infrastructure.Mvc;

/// <summary>
/// 將未被應用程式處理的例外轉換成不含內部細節的統一 API 錯誤回應。
/// </summary>
public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    /// <summary>
    /// 初始化 API 例外處理器。
    /// </summary>
    /// <param name="logger">記錄伺服器端例外與 traceId 的 logger。</param>
    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 記錄完整例外後，向 caller 回傳安全的 <c>System.UnexpectedError</c>。
    /// </summary>
    /// <param name="httpContext">目前發生例外的 HTTP request context。</param>
    /// <param name="exception">未被上層處理的例外。</param>
    /// <param name="cancellationToken">Request 中止通知。</param>
    /// <returns>固定回傳 <see langword="true" />，表示例外已轉換為 HTTP response。</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "處理 API request 時發生非預期錯誤。TraceId: {TraceId}",
            httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new ApiErrorResponse
            {
                Code = "System.UnexpectedError",
                Message = "系統暫時無法處理要求，請稍後再試。",
                TraceId = httpContext.TraceIdentifier,
            },
            cancellationToken);

        return true;
    }
}
