namespace FoodLedger.Services;

/// <summary>表示單一 Body Measurement request 欄位違反商業規則。</summary>
public sealed class BodyMeasurementValidationException(string fieldName, string errorCode)
    : Exception(errorCode)
{
    public string FieldName { get; } = fieldName;
    public string ErrorCode { get; } = errorCode;
}
