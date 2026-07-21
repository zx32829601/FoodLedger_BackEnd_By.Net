using FoodLedger.DTOs.DailyRecords;
using FoodLedger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
            ModelState.AddModelError(exception.ParamName ?? string.Empty, exception.Message);
            return ValidationProblem(
                statusCode: StatusCodes.Status400BadRequest,
                modelStateDictionary: ModelState);
        }

        return NoContent();
    }
}
