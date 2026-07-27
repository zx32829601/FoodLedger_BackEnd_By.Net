using FoodLedger.DTOs.DefinedCodes;
using FoodLedger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodLedger.Controllers;

/// <summary>
/// 提供前端固定選項所需的公開通用代碼 API。
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/defined-codes")]
public sealed class DefinedCodesController(IDefinedCodeService definedCodeService) : ControllerBase
{
    /// <summary>
    /// 取得可供飲食紀錄使用的餐別選項。
    /// </summary>
    /// <param name="cancellationToken">取消目前 HTTP request 的通知權杖。</param>
    /// <returns>回傳 <c>200 OK</c> 與啟用餐別，結果依顯示順序排列。</returns>
    /// <example>
    /// <code>
    /// GET /api/defined-codes/meal-types
    /// </code>
    /// </example>
    [HttpGet("meal-types")]
    [ProducesResponseType(typeof(IReadOnlyList<DefinedCodeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DefinedCodeResponse>>> GetMealTypes(
        CancellationToken cancellationToken = default)
    {
        var mealTypes = await definedCodeService.GetActiveMealTypesAsync(cancellationToken);
        return Ok(mealTypes);
    }
}
