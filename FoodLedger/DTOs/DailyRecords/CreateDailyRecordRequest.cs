using System.ComponentModel.DataAnnotations;
using FoodLedger.DTOs.Errors;

namespace FoodLedger.DTOs.DailyRecords;

/// <summary>
/// 建立每日飲食紀錄的請求資料。
/// </summary>
/// <remarks>
/// 請求不可包含使用者 ID；紀錄擁有者必須由後端透過目前登入使用者決定。
/// </remarks>
public sealed class CreateDailyRecordRequest : IValidatableObject
{
    private const decimal MinimumQuantity = 0.001m;

    private const decimal MaximumQuantity = 10000m;

    /// <summary>
    /// 食物資料識別碼。
    /// </summary>
    /// <remarks>
    /// 識別碼必須大於 0，避免無效的食物識別碼進入 Service 流程。
    /// </remarks>
    public long FoodId { get; init; }

    /// <summary>
    /// 食用份量，單位為克。
    /// </summary>
    /// <remarks>
    /// 數量必須介於 0.001 到 10000 之間，避免建立沒有實際攝取量或明顯不合理的飲食紀錄。
    /// </remarks>
    public decimal QuantityInGrams { get; init; }

    /// <summary>
    /// 食用時間，應使用 UTC。
    /// </summary>
    public DateTimeOffset ConsumedAt { get; init; }

    /// <summary>
    /// 餐別代碼，必須是目前啟用的 MealType。
    /// </summary>
    public string MealTypeCode { get; init; } = string.Empty;

    /// <summary>
    /// 飲食紀錄的選填備註，trim 後最多 500 字元。
    /// </summary>
    public string? Note { get; init; }

    /// <summary>
    /// 驗證食物識別碼與食用數量，並回傳可由 API 層轉換的穩定錯誤代碼。
    /// </summary>
    /// <param name="validationContext">目前 request 的驗證內容。</param>
    /// <returns>欄位不符合限制時回傳對應的驗證結果。</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FoodId <= 0)
        {
            yield return new ValidationResult(
                DailyRecordErrorCodes.FoodIdInvalid,
                [nameof(FoodId)]);
        }

        if (QuantityInGrams <= 0)
        {
            yield return new ValidationResult(
                DailyRecordErrorCodes.QuantityMustBeGreaterThanZero,
                [nameof(QuantityInGrams)]);
        }
        else if (QuantityInGrams is < MinimumQuantity or > MaximumQuantity)
        {
            yield return new ValidationResult(
                DailyRecordErrorCodes.QuantityOutOfRange,
                [nameof(QuantityInGrams)]);
        }

        if (string.IsNullOrWhiteSpace(MealTypeCode))
        {
            yield return new ValidationResult(
                DailyRecordErrorCodes.InvalidMealType,
                [nameof(MealTypeCode)]);
        }

        if (Note?.Trim().Length > DailyRecordRules.MaximumNoteLength)
        {
            yield return new ValidationResult(
                DailyRecordErrorCodes.NoteTooLong,
                [nameof(Note)]);
        }
    }
}
