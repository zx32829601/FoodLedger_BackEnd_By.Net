using FoodLedger.Controllers;
using FoodLedger.DTOs.DailyRecords;
using FoodLedger.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodLedger.Tests.Controllers;

/// <summary>
/// 驗證 <see cref="DailyRecordsController" /> 對每日飲食紀錄 API 的 HTTP 邊界行為。
/// </summary>
public class DailyRecordsControllerTests
{
    /// <summary>
    /// 驗證 Service 回報欄位超出允許範圍時，Controller 會轉成 400 ValidationProblem，避免例外外漏成 500。
    /// </summary>
    [Test]
    public async Task Create_WhenServiceThrowsArgumentOutOfRangeException_ReturnsBadRequestValidationProblem()
    {
        // Arrange
        var dailyRecordService = new ThrowingDailyRecordService(
            new ArgumentOutOfRangeException(nameof(CreateDailyRecordRequest.ConsumedAt)));
        var controller = new DailyRecordsController(dailyRecordService);
        var request = new CreateDailyRecordRequest
        {
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = DateTimeOffset.UtcNow,
        };

        // Act
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        var validationProblemResult = result as ObjectResult;
        Assert.That(validationProblemResult, Is.Not.Null);
        Assert.That(validationProblemResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(validationProblemResult.Value, Is.TypeOf<ValidationProblemDetails>());

        var problemDetails = (ValidationProblemDetails)validationProblemResult.Value!;
        Assert.That(problemDetails.Errors, Contains.Key(nameof(CreateDailyRecordRequest.ConsumedAt)));
    }

    private sealed class ThrowingDailyRecordService : IDailyRecordService
    {
        private readonly Exception _exception;

        public ThrowingDailyRecordService(Exception exception)
        {
            _exception = exception;
        }

        public Task CreateDailyRecordAsync(
            CreateDailyRecordRequest request,
            CancellationToken cancellationToken = default)
        {
            throw _exception;
        }
    }
}
