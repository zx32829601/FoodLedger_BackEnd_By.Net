using FoodLedger.Data;
using FoodLedger.Infrastructure.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Controllers;

/// <summary>
/// 提供 Development 環境使用的資料庫連線檢查端點。
/// </summary>
/// <remarks>
/// 此 Controller 僅供本機開發診斷，非 Development 環境會被 MVC 註冊流程排除。
/// </remarks>
[ApiController]
[DevelopmentOnlyController]
[Route("api/[controller]")]
public class TestDbController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// 建立資料庫連線檢查 Controller。
    /// </summary>
    /// <param name="context">應用程式資料庫內容，用於檢查資料庫是否可查詢。</param>
    public TestDbController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 讀取第一筆飲食紀錄以確認資料庫連線狀態。
    /// </summary>
    /// <returns>連線成功訊息、第一筆飲食紀錄，或連線失敗錯誤訊息。</returns>
    /// <remarks>
    /// 此 action 僅供 Development 環境手動檢查使用，不應作為正式健康檢查或業務 API。
    /// </remarks>
    [HttpGet("check-connection")]
    public async Task<IActionResult> GetFirstRecord()
    {
        try
        {
            // 試著抓取第一筆紀錄。
            var record = await _context.DailyRecords.FirstOrDefaultAsync();

            if (record == null)
            {
                return Ok("連線成功！但資料庫裡還沒有任何飲食紀錄。");
            }

            return Ok(new { Message = "連線成功！撈到一筆資料：", Data = record });
        }
        catch (Exception ex)
        {
            return BadRequest($"連線失敗！錯誤訊息：{ex.Message}");
        }
    }
}
