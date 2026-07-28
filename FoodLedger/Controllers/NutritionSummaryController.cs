using FoodLedger.DTOs.Nutrition;
using FoodLedger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodLedger.Controllers;

/// <summary>
/// 提供目前登入使用者的營養攝取統計。
/// </summary>
[ApiController]
[Authorize]
[Route("api/nutrition-summary")]
public sealed class NutritionSummaryController(INutritionSummaryService service) : ControllerBase
{
    /// <summary>查詢指定本地日期的營養攝取總量與餐別 breakdown。</summary>
    /// <param name="request">本地日期、IANA 時區與 BCP 47 語系。</param>
    /// <param name="cancellationToken">取消 request 的通知權杖。</param>
    /// <returns>每日動態營養素總量。</returns>
    /// <example>
    /// GET /api/nutrition-summary/daily?date=2026-07-28&amp;timeZone=Asia%2FTaipei&amp;langCode=zh-TW
    /// </example>
    [HttpGet("daily")]
    [ProducesResponseType(typeof(DailyNutritionSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DailyNutritionSummaryResponse>> GetDailyAsync(
        [FromQuery] NutritionSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await service.GetDailyAsync(
            request.Date!.Value,
            request.TimeZone,
            request.LangCode,
            cancellationToken));
    }

    /// <summary>查詢焦點日期所在週一至週日的營養摘要。</summary>
    /// <param name="request">焦點日期、IANA 時區與 BCP 47 語系。</param>
    /// <param name="cancellationToken">取消 request 的通知權杖。</param>
    /// <returns>整週總量與固定七天 breakdown。</returns>
    /// <example>
    /// GET /api/nutrition-summary/weekly?date=2026-07-29&amp;timeZone=Asia%2FTaipei&amp;langCode=zh-TW
    /// </example>
    [HttpGet("weekly")]
    [ProducesResponseType(typeof(WeeklyNutritionSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WeeklyNutritionSummaryResponse>> GetWeeklyAsync(
        [FromQuery] NutritionSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await service.GetWeeklyAsync(
            request.Date!.Value,
            request.TimeZone,
            request.LangCode,
            cancellationToken));
    }
}
