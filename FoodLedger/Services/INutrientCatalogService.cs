using FoodLedger.DTOs.Nutrition;

namespace FoodLedger.Services;

/// <summary>
/// 定義營養素多語系目錄查詢。
/// </summary>
public interface INutrientCatalogService
{
    /// <summary>
    /// 依指定語系取得所有營養素的顯示名稱與單位。
    /// </summary>
    /// <param name="langCode">BCP 47 語系代碼。</param>
    /// <param name="cancellationToken">取消查詢的通知權杖。</param>
    /// <returns>依穩定營養素代碼排序的目錄。</returns>
    Task<IReadOnlyList<NutrientCatalogItemResponse>> GetAsync(
        string langCode,
        CancellationToken cancellationToken = default);
}
