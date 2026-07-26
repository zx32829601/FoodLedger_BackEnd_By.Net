namespace FoodLedger.DTOs.Errors;

/// <summary>
/// 描述單一 request 欄位的穩定錯誤代碼與 fallback 訊息。
/// </summary>
public sealed class ApiFieldError
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
    /// 多語系文案需要插入限制值或識別資料時使用的參數。
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Parameters { get; init; }
}
