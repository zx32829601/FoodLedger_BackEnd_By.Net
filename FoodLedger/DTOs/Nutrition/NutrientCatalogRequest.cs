using System.ComponentModel.DataAnnotations;
using FoodLedger.DTOs.Errors;
using FoodLedger.DTOs.Foods;
using FoodLedger.Models;

namespace FoodLedger.DTOs.Nutrition;

/// <summary>
/// 營養素目錄的在地化查詢參數。
/// </summary>
public sealed class NutrientCatalogRequest : IValidatableObject
{
    /// <summary>營養素顯示名稱使用的 BCP 47 語系代碼。</summary>
    [Required(ErrorMessage = FoodSearchErrorCodes.InvalidLangCode)]
    public string LangCode { get; init; } = FoodSearchRequest.DefaultLangCode;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!LocalizationRules.IsValidLangCode(LangCode))
        {
            yield return new ValidationResult(
                FoodSearchErrorCodes.InvalidLangCode,
                [nameof(LangCode)]);
        }
    }
}
