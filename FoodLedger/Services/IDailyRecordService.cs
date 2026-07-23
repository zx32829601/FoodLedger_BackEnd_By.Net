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
    /// 查詢目前登入使用者在指定 UTC 日期內的每日飲食紀錄。
    /// </summary>
    /// <param name="date">要查詢的 UTC 日期。</param>
    /// <param name="cancellationToken">取消查詢作業的通知權杖。</param>
    /// <returns>符合日期與目前登入使用者的每日飲食紀錄清單。</returns>
    Task<IReadOnlyList<DailyRecordResponse>> GetDailyRecordsAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);
}
