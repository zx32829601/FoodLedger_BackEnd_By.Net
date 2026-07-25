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
    /// 驗證查詢飲食紀錄成功時，Controller 會將日期與取消權杖交給 Service，並回傳 200 OK 與紀錄清單。
    /// </summary>
    [Test]
    public async Task GetDailyRecords_WhenServiceReturnsRecords_ReturnsOkWithRecords()
    {
        // Arrange
        var expectedRecords = new[]
        {
            new DailyRecordResponse
            {
                RecordId = 1,
                FoodId = 2,
                Quantity = 1.5m,
                ConsumedAt = new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero),
            },
        };
        var dailyRecordService = new RecordingDailyRecordService
        {
            RecordsToReturn = expectedRecords,
        };
        var controller = new DailyRecordsController(dailyRecordService);
        var date = new DateOnly(2026, 7, 23);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Act
        var result = await controller.GetDailyRecords(date, cancellationToken);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value, Is.SameAs(expectedRecords));
            Assert.That(dailyRecordService.ReceivedDate, Is.EqualTo(date));
            Assert.That(dailyRecordService.ReceivedGetCancellationToken, Is.EqualTo(cancellationToken));
        });
    }

    /// <summary>
    /// 驗證查詢飲食紀錄時 Service 回報未授權，Controller 會轉成 401 Unauthorized。
    /// </summary>
    [Test]
    public async Task GetDailyRecords_WhenServiceThrowsUnauthorizedAccessException_ReturnsUnauthorized()
    {
        // Arrange
        var dailyRecordService = new ThrowingDailyRecordService(new UnauthorizedAccessException());
        var controller = new DailyRecordsController(dailyRecordService);

        // Act
        var result = await controller.GetDailyRecords(new DateOnly(2026, 7, 23), CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<UnauthorizedResult>());
    }

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

    /// <summary>
    /// 驗證 Service 回報目前 request 沒有可用的登入使用者時，Controller 會轉成 401 Unauthorized。
    /// </summary>
    [Test]
    public async Task Create_WhenServiceThrowsUnauthorizedAccessException_ReturnsUnauthorized()
    {
        // Arrange
        var dailyRecordService = new ThrowingDailyRecordService(new UnauthorizedAccessException());
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
        Assert.That(result, Is.TypeOf<UnauthorizedResult>());
    }

    /// <summary>
    /// 驗證刪除飲食紀錄 request 有效時，Controller 會將紀錄識別碼與取消權杖交給 Service，並回傳 204 No Content。
    /// </summary>
    [Test]
    public async Task Delete_WhenRequestIsValid_CallsServiceAndReturnsNoContent()
    {
        // Arrange
        var dailyRecordService = new RecordingDailyRecordService();
        var controller = new DailyRecordsController(dailyRecordService);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Act
        var result = await controller.Delete(1, cancellationToken);

        // Assert
        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.Multiple(() =>
        {
            Assert.That(dailyRecordService.ReceivedDeleteRecordId, Is.EqualTo(1));
            Assert.That(dailyRecordService.ReceivedDeleteCancellationToken, Is.EqualTo(cancellationToken));
        });
    }

    /// <summary>
    /// 驗證刪除飲食紀錄時 Service 回報未授權，Controller 會轉成 401 Unauthorized。
    /// </summary>
    [Test]
    public async Task Delete_WhenServiceThrowsUnauthorizedAccessException_ReturnsUnauthorized()
    {
        // Arrange
        var dailyRecordService = new ThrowingDailyRecordService(new UnauthorizedAccessException());
        var controller = new DailyRecordsController(dailyRecordService);

        // Act
        var result = await controller.Delete(1, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<UnauthorizedResult>());
    }

    private sealed class RecordingDailyRecordService : IDailyRecordService
    {
        public CreateDailyRecordRequest? ReceivedRequest { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public DateOnly? ReceivedDate { get; private set; }

        public CancellationToken ReceivedGetCancellationToken { get; private set; }

        public long? ReceivedDeleteRecordId { get; private set; }

        public CancellationToken ReceivedDeleteCancellationToken { get; private set; }

        public IReadOnlyList<DailyRecordResponse> RecordsToReturn { get; init; } = [];

        public Task CreateDailyRecordAsync(
            CreateDailyRecordRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedRequest = request;
            ReceivedCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DailyRecordResponse>> GetDailyRecordsAsync(
            DateOnly date,
            CancellationToken cancellationToken = default)
        {
            ReceivedDate = date;
            ReceivedGetCancellationToken = cancellationToken;
            return Task.FromResult(RecordsToReturn);
        }

        public Task DeleteDailyRecordAsync(
            long recordId,
            CancellationToken cancellationToken = default)
        {
            ReceivedDeleteRecordId = recordId;
            ReceivedDeleteCancellationToken = cancellationToken;
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

        public Task<IReadOnlyList<DailyRecordResponse>> GetDailyRecordsAsync(
            DateOnly date,
            CancellationToken cancellationToken = default)
        {
            throw _exception;
        }

        public Task DeleteDailyRecordAsync(
            long recordId,
            CancellationToken cancellationToken = default)
        {
            throw _exception;
        }
    }
}
