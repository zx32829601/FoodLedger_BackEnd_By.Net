namespace FoodLedger.Services;

/// <summary>
/// 表示身體資料的單一欄位不符合業務規則。
/// </summary>
public sealed class BodyProfileValidationException(string fieldName, string errorCode)
    : Exception(errorCode)
{
    public string FieldName { get; } = fieldName;
    public string ErrorCode { get; } = errorCode;
}
