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
    /// 驗證新增飲食紀錄 request 有效時，Controller 會將同一份 request 與取消權杖交給 Service，並回傳 204 No Content。
    /// </summary>
    [Test]
    public async Task Create_WhenRequestIsValid_CallsServiceAndReturnsNoContent()
    {
        // Arrange
        var dailyRecordService = new RecordingDailyRecordService();
        var controller = new DailyRecordsController(dailyRecordService);
        var request = new CreateDailyRecordRequest
        {
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = DateTimeOffset.UtcNow,
        };
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Act
        var result = await controller.Create(request, cancellationToken);

        // Assert
        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.Multiple(() =>
        {
            Assert.That(dailyRecordService.ReceivedRequest, Is.SameAs(request));
            Assert.That(dailyRecordService.ReceivedCancellationToken, Is.EqualTo(cancellationToken));
        });
    }

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

    /// <summary>
    /// 驗證 Service 回報指定資源不存在時，Controller 會轉成 404 Not Found，避免例外外漏成 500。
    /// </summary>
    [Test]
    public async Task Create_WhenServiceThrowsKeyNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var dailyRecordService = new ThrowingDailyRecordService(new KeyNotFoundException("Food 999 does not exist."));
        var controller = new DailyRecordsController(dailyRecordService);
        var request = new CreateDailyRecordRequest
        {
            FoodId = 999,
            Quantity = 1,
            ConsumedAt = DateTimeOffset.UtcNow,
        };

        // Act
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    private sealed class RecordingDailyRecordService : IDailyRecordService
    {
        public CreateDailyRecordRequest? ReceivedRequest { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task CreateDailyRecordAsync(
            CreateDailyRecordRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedRequest = request;
            ReceivedCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
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
