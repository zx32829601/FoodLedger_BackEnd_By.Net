using FoodLedger.DTOs.BodyProfiles;
using FoodLedger.DTOs.DefinedCodes;
using FoodLedger.DTOs.Errors;
using FoodLedger.Infrastructure.Authentication;
using FoodLedger.Infrastructure.Mvc;
using FoodLedger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodLedger.Controllers;

/// <summary>
/// 管理目前登入使用者的唯一身體資料。
/// </summary>
[ApiController]
[Authorize]
[Route("api/me/body-profile")]
public sealed class BodyProfilesController(IBodyProfileService service) : ControllerBase
{
    /// <summary>取得目前登入使用者的身體資料與在地化選項名稱。</summary>
    /// <param name="request">包含回應語系的查詢參數。</param>
    /// <param name="cancellationToken">取消 request 的通知權杖。</param>
    /// <returns>身體資料；尚未建立時回傳 404，未登入時回傳 401。</returns>
    [HttpGet]
    [ProducesResponseType(typeof(BodyProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        [FromQuery] DefinedCodeQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await service.GetAsync(request.LangCode, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateError(
                BodyProfileErrorCodes.NotFound,
                "尚未建立身體資料。"));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>建立或更新目前登入使用者的唯一身體資料。</summary>
    /// <param name="request">身體資料、選項代碼、時區與並行版本。</param>
    /// <param name="cancellationToken">取消 request 的通知權杖。</param>
    /// <returns>最新身體資料；驗證失敗回傳 400，版本衝突回傳 409。</returns>
    /// <remarks>
    /// 更新既有資料時必須帶入目前版本，避免後送出的舊畫面覆蓋較新的修改。
    /// </remarks>
    [HttpPut]
    [CookieAntiforgery]
    [ProducesResponseType(typeof(BodyProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Upsert(
        UpsertBodyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await service.UpsertAsync(request, cancellationToken));
        }
        catch (BodyProfileValidationException exception)
        {
            return ApiValidationProblemFactory.CreateForField(
                HttpContext,
                exception.FieldName,
                exception.ErrorCode);
        }
        catch (BodyProfileConflictException)
        {
            return Conflict(CreateError(
                BodyProfileErrorCodes.Conflict,
                "身體資料已被更新，請重新讀取後再試。"));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    private ApiErrorResponse CreateError(string code, string message) => new()
    {
        Code = code,
        Message = message,
        TraceId = HttpContext.TraceIdentifier,
    };
}
