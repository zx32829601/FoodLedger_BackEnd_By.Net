namespace FoodLedger.Services;

/// <summary>
/// 表示食物維護 request 與目前資料狀態不相容。
/// </summary>
public sealed class FoodMaintenanceValidationException(string fieldName, string errorCode)
    : Exception(errorCode)
{
    public string FieldName { get; } = fieldName;
    public string ErrorCode { get; } = errorCode;
}
