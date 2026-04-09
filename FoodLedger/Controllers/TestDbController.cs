using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodLedger.Data;

namespace FoodLedger.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestDbController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TestDbController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("check-connection")]
    public async Task<IActionResult> GetFirstRecord()
    {
        try
        {
            // 試著抓取第一筆紀錄
            var record = await _context.DailyRecords.FirstOrDefaultAsync();

            if (record == null)
                return Ok("連線成功！但資料庫裡還沒有任何飲食紀錄。");

            return Ok(new { Message = "連線成功！撈到一筆資料：", Data = record });
        }
        catch (Exception ex)
        {
            return BadRequest($"連線失敗！錯誤訊息：{ex.Message}");
        }
    }
}