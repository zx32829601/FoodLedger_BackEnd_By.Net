using FoodLedger.DTOs.BodyMeasurements;
using FoodLedger.DTOs.Errors;
using FoodLedger.Infrastructure.Authentication;
using FoodLedger.Infrastructure.Mvc;
using FoodLedger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodLedger.Controllers;

/// <summary>管理目前登入使用者的身體測量歷史。</summary>
[ApiController]
[Authorize]
[Route("api/me/body-measurements")]
public sealed class BodyMeasurementsController(IBodyMeasurementService service) : ControllerBase
{
    /// <summary>依本地日期與分頁條件取得身體測量歷史。</summary>
    [HttpGet]
    [ProducesResponseType(typeof(BodyMeasurementPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] BodyMeasurementQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await service.GetHistoryAsync(request, cancellationToken));
        }
        catch (BodyMeasurementProfileRequiredException)
        {
            return UnprocessableEntity(CreateError(
                BodyMeasurementErrorCodes.ProfileRequired,
                "使用本地日期篩選前，請先建立身體資料並設定時區。"));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>以伺服器目前時間新增一筆身體測量。</summary>
    [HttpPost]
    [CookieAntiforgery]
    [ProducesResponseType(typeof(BodyMeasurementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        CreateBodyMeasurementRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await service.CreateAsync(request, cancellationToken);
            return Created($"/api/me/body-measurements/{result.MeasurementId}", result);
        }
        catch (BodyMeasurementValidationException exception)
        {
            return ApiValidationProblemFactory.CreateForField(
                HttpContext,
                exception.FieldName,
                exception.ErrorCode);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>以樂觀並行版本修正一筆身體測量值。</summary>
    [HttpPut("{measurementId:long}")]
    [CookieAntiforgery]
    [ProducesResponseType(typeof(BodyMeasurementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(
        long measurementId,
        UpdateBodyMeasurementRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await service.UpdateAsync(measurementId, request, cancellationToken));
        }
        catch (BodyMeasurementValidationException exception)
        {
            return ApiValidationProblemFactory.CreateForField(
                HttpContext,
                exception.FieldName,
                exception.ErrorCode);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateNotFoundError());
        }
        catch (BodyMeasurementConflictException)
        {
            return Conflict(CreateConflictError());
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>取得刪除前的影響預覽與短效確認 token。</summary>
    [HttpGet("{measurementId:long}/deletion-impact")]
    [ProducesResponseType(typeof(BodyMeasurementDeletionImpactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDeletionImpact(
        long measurementId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await service.GetDeletionImpactAsync(measurementId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateNotFoundError());
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>使用影響預覽 token 與目前版本永久刪除一筆身體測量。</summary>
    [HttpDelete("{measurementId:long}")]
    [CookieAntiforgery]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(
        long measurementId,
        DeleteBodyMeasurementRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await service.DeleteAsync(measurementId, request, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateNotFoundError());
        }
        catch (BodyMeasurementConflictException)
        {
            return Conflict(CreateConflictError());
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    private ApiErrorResponse CreateNotFoundError() => CreateError(
        BodyMeasurementErrorCodes.NotFound,
        "找不到指定的身體測量。"
    );

    private ApiErrorResponse CreateConflictError() => CreateError(
        BodyMeasurementErrorCodes.Conflict,
        "身體測量版本或刪除確認已失效，請重新讀取後再試。"
    );

    private ApiErrorResponse CreateError(string code, string message) => new()
    {
        Code = code,
        Message = message,
        TraceId = HttpContext.TraceIdentifier,
    };
}
