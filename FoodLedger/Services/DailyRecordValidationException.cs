namespace FoodLedger.Services;

/// <summary>
/// 表示飲食紀錄的單一欄位不符合商業驗證規則。
/// </summary>
public sealed class DailyRecordValidationException(
    string fieldName,
    string errorCode) : Exception(errorCode)
{
    /// <summary>
    /// 驗證失敗的 request 欄位名稱。
    /// </summary>
    public string FieldName { get; } = fieldName;

    /// <summary>
    /// 可供 API 回傳的穩定錯誤代碼。
    /// </summary>
    public string ErrorCode { get; } = errorCode;
}
