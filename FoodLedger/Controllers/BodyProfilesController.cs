using FoodLedger.DTOs.BodyProfiles;
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
    [HttpGet]
    [ProducesResponseType(typeof(BodyProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await service.GetAsync(cancellationToken));
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
