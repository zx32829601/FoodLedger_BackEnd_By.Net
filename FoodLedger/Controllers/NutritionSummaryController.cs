using FoodLedger.DTOs.Nutrition;
using FoodLedger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FoodLedger.Controllers;

/// <summary>
/// 提供目前登入使用者的營養攝取統計。
/// </summary>
[ApiController]
[Authorize]
[Route("api/nutrition-summary")]
public sealed class NutritionSummaryController(INutritionSummaryService service) : ControllerBase
{
    /// <summary>查詢指定 UTC 日期的營養攝取總量。</summary>
    /// <param name="date">要查詢的日期。</param>
    /// <param name="cancellationToken">取消 request 的通知權杖。</param>
    /// <returns>每日動態營養素總量。</returns>
    [HttpGet("daily")]
    [ProducesResponseType(typeof(DailyNutritionSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DailyNutritionSummaryResponse>> GetDailyAsync(
        [FromQuery, BindRequired] DateOnly date,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetDailyAsync(date, cancellationToken));
    }
}
