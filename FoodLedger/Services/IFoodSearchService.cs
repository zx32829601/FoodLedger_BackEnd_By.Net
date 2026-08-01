using FoodLedger.DTOs.Foods;

namespace FoodLedger.Services;

/// <summary>
/// 提供使用者食物搜尋功能。
/// </summary>
public interface IFoodSearchService
{
    /// <summary>
    /// 依名稱與語系搜尋食物並回傳分頁結果。
    /// </summary>
    /// <param name="request">搜尋、語系與分頁條件。</param>
    /// <param name="cancellationToken">取消查詢作業的通知權杖。</param>
    /// <returns>符合條件的食物分頁結果。</returns>
    Task<FoodSearchResponse> SearchAsync(
        FoodSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>依識別碼取得指定語系的完整食物明細。</summary>
    /// <param name="foodId">食物識別碼。</param>
    /// <param name="langCode">BCP 47 顯示語系。</param>
    /// <param name="cancellationToken">取消目前資料庫查詢的通知權杖。</param>
    /// <returns>可顯示的食物明細；食物或可用名稱不存在時為 null。</returns>
    Task<FoodDetailResponse?> GetAsync(
        long foodId,
        string langCode,
        CancellationToken cancellationToken = default);
}
