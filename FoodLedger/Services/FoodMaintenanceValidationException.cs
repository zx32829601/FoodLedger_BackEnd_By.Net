namespace FoodLedger.Services;

/// <summary>
/// 表示食物維護 request 與目前資料狀態不相容。
/// </summary>
public sealed class FoodMaintenanceValidationException(string fieldName, string errorCode)
    : Exception(errorCode)
{
    /// <summary>取得驗證失敗的 API 欄位名稱。</summary>
    public string FieldName { get; } = fieldName;

    /// <summary>取得可供前端穩定判斷的錯誤代碼。</summary>
    public string ErrorCode { get; } = errorCode;
}
