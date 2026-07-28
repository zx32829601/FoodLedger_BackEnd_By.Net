using System.ComponentModel.DataAnnotations;
using FoodLedger.DTOs.Errors;
using FoodLedger.DTOs.Foods;
using FoodLedger.Models;

namespace FoodLedger.DTOs.Nutrition;

/// <summary>
/// 每日與每週營養摘要共用的查詢參數。
/// </summary>
public sealed class NutritionSummaryRequest : IValidatableObject
{
    /// <summary>使用者選擇的本地日曆日期。</summary>
    [Required(ErrorMessage = NutritionSummaryErrorCodes.DateRequired)]
    public DateOnly? Date { get; init; }

    /// <summary>切分本地日界的 IANA timezone。</summary>
    [Required(ErrorMessage = NutritionSummaryErrorCodes.InvalidTimeZone)]
    public string TimeZone { get; init; } = string.Empty;

    /// <summary>營養素名稱使用的 BCP 47 語系代碼。</summary>
    [Required(ErrorMessage = NutritionSummaryErrorCodes.InvalidLangCode)]
    public string LangCode { get; init; } = FoodSearchRequest.DefaultLangCode;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(TimeZone)
            && !LocalizationRules.IsValidTimeZone(TimeZone))
        {
            yield return new ValidationResult(
                NutritionSummaryErrorCodes.InvalidTimeZone,
                [nameof(TimeZone)]);
        }

        if (!LocalizationRules.IsValidLangCode(LangCode))
        {
            yield return new ValidationResult(
                NutritionSummaryErrorCodes.InvalidLangCode,
                [nameof(LangCode)]);
        }
    }

}
