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
    /// <param name="cancellationToken">取消查詢作業的通知權杖。</param>
    /// <returns>依顯示順序排列的啟用餐別。</returns>
    Task<IReadOnlyList<DefinedCodeResponse>> GetActiveMealTypesAsync(
        CancellationToken cancellationToken = default);
}
