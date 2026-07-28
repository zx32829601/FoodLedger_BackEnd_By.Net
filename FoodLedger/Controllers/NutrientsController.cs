using FoodLedger.DTOs.Errors;
using FoodLedger.DTOs.Nutrition;
using FoodLedger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodLedger.Controllers;

/// <summary>
/// 提供建立與編輯食物時使用的在地化營養素目錄。
/// </summary>
[ApiController]
[Authorize]
[Route("api/nutrients")]
public sealed class NutrientsController(INutrientCatalogService service) : ControllerBase
{
    /// <summary>取得建立與編輯食物時使用的在地化營養素目錄。</summary>
    /// <param name="request">BCP 47 語系查詢參數。</param>
    /// <param name="cancellationToken">取消 request 的通知權杖。</param>
    /// <returns>所有營養素的穩定代碼、翻譯名稱與單位。</returns>
    /// <example>
    /// GET /api/nutrients?langCode=zh-TW
    /// </example>
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<NutrientCatalogItemResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<NutrientCatalogItemResponse>>> GetAsync(
        [FromQuery] NutrientCatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await service.GetAsync(request.LangCode, cancellationToken));
    }
}
