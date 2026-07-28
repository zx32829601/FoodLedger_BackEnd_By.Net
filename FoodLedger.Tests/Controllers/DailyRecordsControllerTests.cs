using FoodLedger.Controllers;
using FoodLedger.DTOs.DailyRecords;
using FoodLedger.DTOs.Errors;
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
        var result = await controller.GetDailyRecords(
            new DailyRecordQueryRequest
            {
                Date = date,
                TimeZone = "Etc/UTC",
                LangCode = "zh-TW",
            },
            cancellationToken);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value, Is.SameAs(expectedRecords));
            Assert.That(dailyRecordService.ReceivedDate, Is.EqualTo(date));
            Assert.That(dailyRecordService.ReceivedTimeZone, Is.EqualTo("Etc/UTC"));
            Assert.That(dailyRecordService.ReceivedLangCode, Is.EqualTo("zh-TW"));
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
        var result = await controller.GetDailyRecords(
            new DailyRecordQueryRequest
            {
                Date = new DateOnly(2026, 7, 23),
                TimeZone = "Etc/UTC",
                LangCode = "zh-TW",
            },
            CancellationToken.None);

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
        var controller = new DailyRecordsController(dailyRecordService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };
        var request = new CreateDailyRecordRequest
        {
            FoodId = 1,
            Quantity = 1,
            ConsumedAt = DateTimeOffset.UtcNow,
        };

        // Act
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        var error = badRequestResult?.Value as ApiErrorResponse;
        Assert.Multiple(() =>
        {
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(error?.Code, Is.EqualTo("Validation.Failed"));
            Assert.That(error?.Errors, Contains.Key("consumedAt"));
            Assert.That(
                error?.Errors?["consumedAt"].Single().Code,
                Is.EqualTo("DailyRecord.ConsumedAtCannotBeFuture"));
            Assert.That(error?.TraceId, Is.Not.Null.And.Not.Empty);
        });
    }

    /// <summary>
    /// 驗證 Service 回報指定資源不存在時，Controller 會轉成 404 Not Found，避免例外外漏成 500。
    /// </summary>
    [Test]
    public async Task Create_WhenServiceThrowsKeyNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var dailyRecordService = new ThrowingDailyRecordService(new KeyNotFoundException("Food 999 does not exist."));
        var controller = new DailyRecordsController(dailyRecordService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };
        var request = new CreateDailyRecordRequest
        {
            FoodId = 999,
            Quantity = 1,
            ConsumedAt = DateTimeOffset.UtcNow,
        };

        // Act
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        var error = notFoundResult?.Value as ApiErrorResponse;
        Assert.Multiple(() =>
        {
            Assert.That(notFoundResult, Is.Not.Null);
            Assert.That(error?.Code, Is.EqualTo("DailyRecord.FoodNotFound"));
            Assert.That(error?.Parameters?["foodId"], Is.EqualTo(999));
            Assert.That(error?.TraceId, Is.Not.Null.And.Not.Empty);
        });
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
    /// 驗證修改 request 有效時，Controller 會傳遞紀錄識別碼與 request 並回傳 204。
    /// </summary>
    [Test]
    public async Task Update_WhenRequestIsValid_CallsServiceAndReturnsNoContent()
    {
        // Arrange
        var dailyRecordService = new RecordingDailyRecordService();
        var controller = new DailyRecordsController(dailyRecordService);
        var request = new UpdateDailyRecordRequest
        {
            FoodId = 2,
            Quantity = 1.5m,
            ConsumedAt = DateTimeOffset.UtcNow,
            MealTypeCode = "Lunch",
            Note = "午餐",
        };

        // Act
        var result = await controller.Update(7, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.Multiple(() =>
        {
            Assert.That(dailyRecordService.ReceivedUpdateRecordId, Is.EqualTo(7));
            Assert.That(dailyRecordService.ReceivedUpdateRequest, Is.SameAs(request));
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

    /// <summary>
    /// 驗證刪除飲食紀錄時 Service 回報指定資源不存在，Controller 會轉成 404 Not Found。
    /// </summary>
    [Test]
    public async Task Delete_WhenServiceThrowsKeyNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var dailyRecordService = new ThrowingDailyRecordService(
            new KeyNotFoundException("DailyRecord 999 does not exist."));
        var controller = new DailyRecordsController(dailyRecordService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };

        // Act
        var result = await controller.Delete(999, CancellationToken.None);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        var error = notFoundResult?.Value as ApiErrorResponse;
        Assert.Multiple(() =>
        {
            Assert.That(notFoundResult, Is.Not.Null);
            Assert.That(error?.Code, Is.EqualTo("DailyRecord.NotFound"));
            Assert.That(error?.Parameters?["recordId"], Is.EqualTo(999));
            Assert.That(error?.TraceId, Is.Not.Null.And.Not.Empty);
        });
    }

    private sealed class RecordingDailyRecordService : IDailyRecordService
    {
        public CreateDailyRecordRequest? ReceivedRequest { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public DateOnly? ReceivedDate { get; private set; }

        public CancellationToken ReceivedGetCancellationToken { get; private set; }

        public long? ReceivedDeleteRecordId { get; private set; }

        public long? ReceivedUpdateRecordId { get; private set; }

        public UpdateDailyRecordRequest? ReceivedUpdateRequest { get; private set; }

        public string? ReceivedTimeZone { get; private set; }

        public string? ReceivedLangCode { get; private set; }

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
            string timeZone,
            string langCode,
            CancellationToken cancellationToken = default)
        {
            ReceivedDate = date;
            ReceivedTimeZone = timeZone;
            ReceivedLangCode = langCode;
            ReceivedGetCancellationToken = cancellationToken;
            return Task.FromResult(RecordsToReturn);
        }

        public Task UpdateDailyRecordAsync(
            long recordId,
            UpdateDailyRecordRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedUpdateRecordId = recordId;
            ReceivedUpdateRequest = request;
            return Task.CompletedTask;
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
            string timeZone,
            string langCode,
            CancellationToken cancellationToken = default)
        {
            throw _exception;
        }

        public Task UpdateDailyRecordAsync(
            long recordId,
            UpdateDailyRecordRequest request,
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
