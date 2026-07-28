using FoodLedger.DTOs.Nutrition;

namespace FoodLedger.Services;

/// <summary>
/// 定義目前使用者的營養攝取彙總查詢。
/// </summary>
public interface INutritionSummaryService
{
    /// <summary>
    /// 依本地日期、IANA 時區與語系查詢單日營養摘要。
    /// </summary>
    /// <param name="date">使用者選擇的本地日曆日期。</param>
    /// <param name="timeZone">切分本地日界的 IANA timezone。</param>
    /// <param name="langCode">營養素名稱使用的 BCP 47 語系代碼。</param>
    /// <param name="cancellationToken">取消查詢的通知權杖。</param>
    /// <returns>當日總量與餐別 breakdown。</returns>
    Task<DailyNutritionSummaryResponse> GetDailyAsync(
        DateOnly date,
        string timeZone,
        string langCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢焦點日期所在週一至週日的營養摘要。
    /// </summary>
    /// <param name="focusDate">用來決定週期的本地日曆日期。</param>
    /// <param name="timeZone">切分本地日界的 IANA timezone。</param>
    /// <param name="langCode">營養素名稱使用的 BCP 47 語系代碼。</param>
    /// <param name="cancellationToken">取消查詢的通知權杖。</param>
    /// <returns>整週總量與固定七天 breakdown。</returns>
    Task<WeeklyNutritionSummaryResponse> GetWeeklyAsync(
        DateOnly focusDate,
        string timeZone,
        string langCode,
        CancellationToken cancellationToken = default);
}
