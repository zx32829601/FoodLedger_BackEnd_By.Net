using FoodLedger.DTOs.DailyRecords;
using FoodLedger.DTOs.Errors;
using FoodLedger.Infrastructure.Mvc;
using FoodLedger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FoodLedger.Controllers;

/// <summary>
/// 提供目前登入使用者的每日飲食紀錄 API。
/// </summary>
/// <remarks>
/// 此 Controller 只負責 HTTP 邊界、授權與錯誤狀態碼轉換；新增紀錄的商業規則由
/// <see cref="IDailyRecordService" /> 負責，避免 Controller 直接處理使用者隔離與資料寫入規則。
/// </remarks>
[ApiController]
[Authorize]
[Route("api/daily-records")]
public sealed class DailyRecordsController : ControllerBase
{
    private readonly IDailyRecordService _dailyRecordService;

    /// <summary>
    /// 建立每日飲食紀錄 Controller。
    /// </summary>
    /// <param name="dailyRecordService">負責每日飲食紀錄新增規則的 Service。</param>
    public DailyRecordsController(IDailyRecordService dailyRecordService)
    {
        _dailyRecordService = dailyRecordService;
    }

    /// <summary>
    /// 查詢目前登入使用者在指定 UTC 日期內的每日飲食紀錄。
    /// </summary>
    /// <param name="date">要查詢的 UTC 日期，格式建議使用 <c>yyyy-MM-dd</c>。</param>
    /// <param name="cancellationToken">取消目前 HTTP request 的通知權杖。</param>
    /// <returns>查詢成功時回傳 <c>200 OK</c> 與飲食紀錄清單。</returns>
    /// <remarks>
    /// 實際可查詢的使用者由登入狀態與 <see cref="IDailyRecordService" /> 決定，前端不需也不應提供 UserId。
    /// </remarks>
    /// <example>
    /// Request:
    /// <code>
    /// GET /api/daily-records?date=2026-07-23
    /// Authorization: Bearer {accessToken}
    /// </code>
    /// </example>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DailyRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDailyRecords(
        [FromQuery, BindRequired] DateOnly date,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var records = await _dailyRecordService.GetDailyRecordsAsync(date, cancellationToken);
            return Ok(records);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// 建立目前登入使用者的一筆每日飲食紀錄。
    /// </summary>
    /// <param name="request">新增飲食紀錄所需的食物、份量與實際攝取時間。</param>
    /// <param name="cancellationToken">取消目前 HTTP request 的通知權杖。</param>
    /// <returns>
    /// 新增成功時回傳 <c>204 No Content</c>；若欄位值超出 Service 允許範圍，回傳
    /// <c>400 ValidationProblem</c>。
    /// </returns>
    /// <remarks>
    /// Request 不應包含 UserId；實際擁有者由登入狀態與 <see cref="IDailyRecordService" /> 決定。
    /// </remarks>
    /// <example>
    /// Request:
    /// <code>
    /// POST /api/daily-records
    /// Authorization: Bearer {accessToken}
    /// {
    ///   "foodId": 1,
    ///   "quantity": 1,
    ///   "consumedAt": "2026-07-21T12:00:00Z"
    /// }
    /// </code>
    /// </example>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        CreateDailyRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dailyRecordService.CreateDailyRecordAsync(request, cancellationToken);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            var errorCode = exception.ParamName switch
            {
                nameof(CreateDailyRecordRequest.Quantity) when request.Quantity <= 0 =>
                    DailyRecordErrorCodes.QuantityMustBeGreaterThanZero,
                nameof(CreateDailyRecordRequest.Quantity) =>
                    DailyRecordErrorCodes.QuantityOutOfRange,
                nameof(CreateDailyRecordRequest.FoodId) =>
                    DailyRecordErrorCodes.FoodIdInvalid,
                nameof(CreateDailyRecordRequest.ConsumedAt) =>
                    DailyRecordErrorCodes.ConsumedAtCannotBeFuture,
                _ => ApiValidationErrorCodes.InvalidValue,
            };
            return ApiValidationProblemFactory.CreateForField(
                HttpContext,
                exception.ParamName ?? string.Empty,
                errorCode);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiErrorResponse
            {
                Code = DailyRecordErrorCodes.FoodNotFound,
                Message = "找不到指定的食物。",
                TraceId = HttpContext.TraceIdentifier,
                Parameters = new Dictionary<string, object?>
                {
                    ["foodId"] = request.FoodId,
                },
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }

        return NoContent();
    }

    /// <summary>
    /// 刪除目前登入使用者的一筆每日飲食紀錄。
    /// </summary>
    /// <param name="recordId">要刪除的每日飲食紀錄識別碼。</param>
    /// <param name="cancellationToken">取消目前 HTTP request 的通知權杖。</param>
    /// <returns>刪除成功時回傳 <c>204 No Content</c>。</returns>
    /// <remarks>
    /// 實際可刪除的資料由登入狀態與 <see cref="IDailyRecordService" /> 決定，前端不需也不應提供 UserId。
    /// </remarks>
    /// <example>
    /// Request:
    /// <code>
    /// DELETE /api/daily-records/1
    /// Authorization: Bearer {accessToken}
    /// </code>
    /// </example>
    [HttpDelete("{recordId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        long recordId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dailyRecordService.DeleteDailyRecordAsync(recordId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiErrorResponse
            {
                Code = DailyRecordErrorCodes.NotFound,
                Message = "找不到指定的飲食紀錄。",
                TraceId = HttpContext.TraceIdentifier,
                Parameters = new Dictionary<string, object?>
                {
                    ["recordId"] = recordId,
                },
            });
        }

        return NoContent();
    }
}
