using FoodLedger.DTOs.DefinedCodes;
using FoodLedger.DTOs.Errors;
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
    /// <param name="request">BCP 47 語系查詢參數。</param>
    /// <param name="cancellationToken">取消目前 HTTP request 的通知權杖。</param>
    /// <returns>回傳 <c>200 OK</c> 與啟用餐別，結果依顯示順序排列。</returns>
    /// <example>
    /// <code>
    /// GET /api/defined-codes/meal-types?langCode=zh-TW
    /// </code>
    /// </example>
    [HttpGet("meal-types")]
    [ProducesResponseType(typeof(IReadOnlyList<DefinedCodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<DefinedCodeResponse>>> GetMealTypes(
        [FromQuery] DefinedCodeQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var mealTypes = await definedCodeService.GetActiveMealTypesAsync(
            request.LangCode,
            cancellationToken);
        return Ok(mealTypes);
    }

    /// <summary>
    /// 取得可選用的健身目標及其在指定語系下的說明。
    /// </summary>
    [HttpGet("fitness-goals")]
    [ProducesResponseType(typeof(IReadOnlyList<DefinedCodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<DefinedCodeResponse>>> GetFitnessGoals(
        [FromQuery] DefinedCodeQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var fitnessGoals = await definedCodeService.GetActiveFitnessGoalsAsync(
            request.LangCode,
            cancellationToken);
        return Ok(fitnessGoals);
    }

    /// <summary>
    /// 取得可選用的活動程度及其在指定語系下的說明。
    /// </summary>
    [HttpGet("activity-levels")]
    [ProducesResponseType(typeof(IReadOnlyList<DefinedCodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<DefinedCodeResponse>>> GetActivityLevels(
        [FromQuery] DefinedCodeQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var activityLevels = await definedCodeService.GetActiveActivityLevelsAsync(
            request.LangCode,
            cancellationToken);
        return Ok(activityLevels);
    }
}
