using System.ComponentModel.DataAnnotations;
using FoodLedger.DTOs.Errors;
using FoodLedger.Models;

namespace FoodLedger.DTOs.DefinedCodes;

/// <summary>
/// DefinedCode 在地化查詢參數。
/// </summary>
public sealed class DefinedCodeQueryRequest : IValidatableObject
{
    /// <summary>
    /// 顯示名稱與說明使用的 BCP 47 語系代碼。
    /// </summary>
    [Required(ErrorMessage = DefinedCodeErrorCodes.InvalidLangCode)]
    public string LangCode { get; init; } = LocalizationRules.DefaultLangCode;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!LocalizationRules.IsValidLangCode(LangCode))
        {
            yield return new ValidationResult(
                DefinedCodeErrorCodes.InvalidLangCode,
                [nameof(LangCode)]);
        }
    }
}
