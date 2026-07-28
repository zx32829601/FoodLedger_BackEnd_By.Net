using FoodLedger.DTOs.Errors;
using FoodLedger.DTOs.Foods;
using FoodLedger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodLedger.Controllers;

/// <summary>
/// 提供已登入使用者食物搜尋 API。
/// </summary>
[ApiController]
[Authorize]
[Route("api/foods")]
public sealed class FoodsController(IFoodSearchService foodSearchService) : ControllerBase
{
    /// <summary>
    /// 依食物名稱搜尋食物並回傳分頁結果。
    /// </summary>
    /// <param name="request">搜尋文字、語系與分頁條件。</param>
    /// <param name="cancellationToken">取消目前 HTTP request 的通知權杖。</param>
    /// <returns>成功時回傳食物分頁、翻譯與每 100 克營養資料。</returns>
    /// <example>
    /// <code>
    /// GET /api/foods?query=雞&amp;langCode=zh-TW&amp;page=1&amp;pageSize=20
    /// </code>
    /// </example>
    [HttpGet]
    [ProducesResponseType(typeof(FoodSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FoodSearchResponse>> SearchAsync(
        [FromQuery] FoodSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await foodSearchService.SearchAsync(request, cancellationToken);
        return Ok(response);
    }
}
