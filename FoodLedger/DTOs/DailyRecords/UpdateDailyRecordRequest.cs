using System.ComponentModel.DataAnnotations;
using FoodLedger.DTOs.Errors;

namespace FoodLedger.DTOs.DailyRecords;

/// <summary>
/// 修改每日飲食紀錄的請求資料。
/// </summary>
public sealed class UpdateDailyRecordRequest : IValidatableObject
{
    private const decimal MinimumQuantity = 0.001m;
    private const decimal MaximumQuantity = 10000m;

    /// <summary>
    /// 食物資料識別碼，必須大於 0 且對應既有食物。
    /// </summary>
    public long FoodId { get; init; }

    /// <summary>
    /// 食用數量，必須介於 0.001 到 10000。
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// 實際食用時間，不可晚於伺服器目前 UTC 時間。
    /// </summary>
    public DateTimeOffset ConsumedAt { get; init; }

    /// <summary>
    /// 餐別代碼，必須是目前啟用的 MealType。
    /// </summary>
    public string MealTypeCode { get; init; } = string.Empty;

    /// <summary>
    /// 選填備註，trim 後最多 500 字元。
    /// </summary>
    public string? Note { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FoodId <= 0)
        {
            yield return new ValidationResult(DailyRecordErrorCodes.FoodIdInvalid, [nameof(FoodId)]);
        }

        if (Quantity <= 0)
        {
            yield return new ValidationResult(
                DailyRecordErrorCodes.QuantityMustBeGreaterThanZero,
                [nameof(Quantity)]);
        }
        else if (Quantity is < MinimumQuantity or > MaximumQuantity)
        {
            yield return new ValidationResult(
                DailyRecordErrorCodes.QuantityOutOfRange,
                [nameof(Quantity)]);
        }

        if (string.IsNullOrWhiteSpace(MealTypeCode))
        {
            yield return new ValidationResult(
                DailyRecordErrorCodes.InvalidMealType,
                [nameof(MealTypeCode)]);
        }

        if (Note?.Trim().Length > DailyRecordRules.MaximumNoteLength)
        {
            yield return new ValidationResult(DailyRecordErrorCodes.NoteTooLong, [nameof(Note)]);
        }
    }
}
