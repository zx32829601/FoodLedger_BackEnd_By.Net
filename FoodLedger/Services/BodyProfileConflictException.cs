namespace FoodLedger.Services;

/// <summary>
/// 表示使用者嘗試以過期版本覆蓋身體資料。
/// </summary>
public sealed class BodyProfileConflictException : Exception;
