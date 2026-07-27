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
}
