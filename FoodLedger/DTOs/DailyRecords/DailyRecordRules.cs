namespace FoodLedger.DTOs.DailyRecords;

/// <summary>
/// 定義新增與修改飲食紀錄共同使用的欄位限制。
/// </summary>
public static class DailyRecordRules
{
    /// <summary>
    /// 備註 trim 後允許的最大字元數。
    /// </summary>
    public const int MaximumNoteLength = 500;
}
