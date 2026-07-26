namespace FoodLedger.DTOs.Errors;

/// <summary>
/// FoodLedger API 對外使用的 code-first 錯誤回應。
/// </summary>
public sealed class ApiErrorResponse
{
    /// <summary>
    /// 前端用來判斷錯誤類型的穩定代碼。
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// 前端尚未提供對應多語系文案時使用的 fallback 訊息。
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// 對應伺服器端 request log 的追蹤識別碼。
    /// </summary>
    public required string TraceId { get; init; }

    /// <summary>
    /// 多語系文案需要插入資源識別碼或限制值時使用的參數。
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Parameters { get; init; }

    /// <summary>
    /// 依 request 欄位分組的驗證錯誤；一般錯誤可省略。
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<ApiFieldError>>? Errors { get; init; }
}
