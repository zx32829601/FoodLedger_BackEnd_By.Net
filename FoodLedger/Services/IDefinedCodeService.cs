using FoodLedger.DTOs.DefinedCodes;

namespace FoodLedger.Services;

/// <summary>
/// 提供通用代碼的唯讀查詢。
/// </summary>
public interface IDefinedCodeService
{
    /// <summary>
    /// 取得目前可供新飲食紀錄使用的餐別。
    /// </summary>
    /// <param name="langCode">顯示名稱與說明使用的 BCP 47 語系代碼。</param>
    /// <param name="cancellationToken">取消查詢作業的通知權杖。</param>
    /// <returns>依顯示順序排列的啟用餐別。</returns>
    Task<IReadOnlyList<DefinedCodeResponse>> GetActiveMealTypesAsync(
        string langCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得可供使用者選擇的健身目標。
    /// </summary>
    /// <param name="langCode">顯示名稱與說明使用的 BCP 47 語系代碼。</param>
    /// <param name="cancellationToken">取消目前查詢的通知權杖。</param>
    /// <returns>已啟用且依顯示順序排列的健身目標。</returns>
    Task<IReadOnlyList<DefinedCodeResponse>> GetActiveFitnessGoalsAsync(
        string langCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得可供使用者選擇的活動程度。
    /// </summary>
    /// <param name="langCode">顯示名稱與說明使用的 BCP 47 語系代碼。</param>
    /// <param name="cancellationToken">取消目前查詢的通知權杖。</param>
    /// <returns>已啟用且依顯示順序排列的活動程度。</returns>
    Task<IReadOnlyList<DefinedCodeResponse>> GetActiveActivityLevelsAsync(
        string langCode,
        CancellationToken cancellationToken = default);
}
