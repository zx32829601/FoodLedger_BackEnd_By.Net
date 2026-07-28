using FoodLedger.DTOs.DailyRecords;

namespace FoodLedger.Services;

/// <summary>
/// 提供每日飲食紀錄相關商業邏輯。
/// </summary>
public interface IDailyRecordService
{
    /// <summary>
    /// 建立目前登入使用者的每日飲食紀錄。
    /// </summary>
    /// <param name="request">建立每日飲食紀錄所需資料，不包含使用者 ID。</param>
    /// <param name="cancellationToken">取消非同步作業的權杖。</param>
    /// <returns>代表非同步建立作業的工作。</returns>
    /// <exception cref="UnauthorizedAccessException">目前 request 沒有可識別的登入使用者時拋出。</exception>
    Task CreateDailyRecordAsync(
        CreateDailyRecordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 依指定本地日期、IANA 時區與語系查詢目前使用者的飲食紀錄。
    /// </summary>
    /// <param name="date">使用者選擇的本地日曆日期。</param>
    /// <param name="timeZone">切分本地日界的 IANA timezone。</param>
    /// <param name="langCode">食物與營養素名稱使用的 BCP 47 語系代碼。</param>
    /// <param name="cancellationToken">取消查詢的通知權杖。</param>
    /// <returns>符合本地日期與目前使用者的飲食紀錄。</returns>
    Task<IReadOnlyList<DailyRecordResponse>> GetDailyRecordsAsync(
        DateOnly date,
        string timeZone,
        string langCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改目前登入使用者的一筆每日飲食紀錄。
    /// </summary>
    /// <param name="recordId">要修改的飲食紀錄識別碼。</param>
    /// <param name="request">完整的修改資料。</param>
    /// <param name="cancellationToken">取消修改作業的通知權杖。</param>
    /// <returns>代表非同步修改作業的工作。</returns>
    Task UpdateDailyRecordAsync(
        long recordId,
        UpdateDailyRecordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 刪除目前登入使用者的一筆每日飲食紀錄。
    /// </summary>
    /// <param name="recordId">要刪除的每日飲食紀錄識別碼。</param>
    /// <param name="cancellationToken">取消刪除作業的通知權杖。</param>
    /// <returns>代表非同步刪除作業的工作。</returns>
    Task DeleteDailyRecordAsync(
        long recordId,
        CancellationToken cancellationToken = default);
}
