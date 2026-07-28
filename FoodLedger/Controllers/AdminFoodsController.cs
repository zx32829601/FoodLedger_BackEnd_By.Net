using FoodLedger.DTOs.Errors;
using FoodLedger.DTOs.Foods;
using FoodLedger.Infrastructure.Authentication;
using FoodLedger.Infrastructure.Mvc;
using FoodLedger.Security;
using FoodLedger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodLedger.Controllers;

/// <summary>
/// 提供管理員食物建立與維護 API。
/// </summary>
[ApiController]
[Authorize(Roles = ApplicationRoles.Admin)]
[Route("api/admin/foods")]
public sealed class AdminFoodsController(IFoodMaintenanceService service) : ControllerBase
{
    private const string GetFoodByIdRouteName = "AdminFoods.GetById";

    /// <summary>取得單一食物的完整維護資料。</summary>
    /// <param name="foodId">食物識別碼。</param>
    /// <param name="cancellationToken">取消 request 的通知權杖。</param>
    /// <returns>存在時回傳食物資料，否則回傳 404。</returns>
    [HttpGet("{foodId:long}", Name = GetFoodByIdRouteName)]
    [ProducesResponseType(typeof(AdminFoodResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminFoodResponse>> GetAsync(
        long foodId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.GetAsync(foodId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>建立食物及其翻譯與營養素資料。</summary>
    /// <param name="request">完整食物資料。</param>
    /// <param name="cancellationToken">取消 request 的通知權杖。</param>
    /// <returns>建立成功時回傳 201 與新食物。</returns>
    [HttpPost]
    [CookieAntiforgery]
    [ProducesResponseType(typeof(AdminFoodResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AdminFoodResponse>> CreateAsync(
        UpsertFoodRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await service.CreateAsync(request, cancellationToken);
            return CreatedAtRoute(
                GetFoodByIdRouteName,
                new { foodId = response.FoodId },
                response);
        }
        catch (FoodMaintenanceValidationException exception)
        {
            return ApiValidationProblemFactory.CreateForField(
                HttpContext,
                exception.FieldName,
                exception.ErrorCode);
        }
    }

    /// <summary>完整取代指定食物的基本、翻譯與營養素資料。</summary>
    /// <param name="foodId">食物識別碼。</param>
    /// <param name="request">完整食物資料。</param>
    /// <param name="cancellationToken">取消 request 的通知權杖。</param>
    /// <returns>更新成功時回傳食物資料，找不到時回傳 404。</returns>
    [HttpPut("{foodId:long}")]
    [CookieAntiforgery]
    [ProducesResponseType(typeof(AdminFoodResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminFoodResponse>> UpdateAsync(
        long foodId,
        UpsertFoodRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.UpdateAsync(foodId, request, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (FoodMaintenanceValidationException exception)
        {
            return ApiValidationProblemFactory.CreateForField(
                HttpContext,
                exception.FieldName,
                exception.ErrorCode);
        }
    }

    /// <summary>刪除尚未被飲食紀錄使用的食物。</summary>
    /// <param name="foodId">食物識別碼。</param>
    /// <param name="cancellationToken">取消 request 的通知權杖。</param>
    /// <returns>刪除成功回傳 204；使用中回傳 409。</returns>
    [HttpDelete("{foodId:long}")]
    [CookieAntiforgery]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(
        long foodId,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteAsync(foodId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException)
        {
            return Conflict(new ApiErrorResponse
            {
                Code = FoodMaintenanceErrorCodes.InUse,
                Message = "此食物已有飲食紀錄，無法刪除。",
                TraceId = HttpContext.TraceIdentifier,
            });
        }
    }
}
