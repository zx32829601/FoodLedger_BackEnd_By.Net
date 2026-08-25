namespace FoodLedger.Services;

/// <summary>
/// 表示身體資料的單一欄位不符合業務規則。
/// </summary>
public sealed class BodyProfileValidationException(string fieldName, string errorCode)
    : Exception(errorCode)
{
    /// <summary>取得驗證失敗的 API 欄位名稱。</summary>
    public string FieldName { get; } = fieldName;

    /// <summary>取得可供前端穩定判斷的錯誤代碼。</summary>
    public string ErrorCode { get; } = errorCode;
}
