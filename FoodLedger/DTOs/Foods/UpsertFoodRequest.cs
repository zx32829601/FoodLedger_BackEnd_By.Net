using System.ComponentModel.DataAnnotations;

namespace FoodLedger.DTOs.Foods;

/// <summary>
/// 建立或完整更新食物資料的 request。
/// </summary>
public sealed class UpsertFoodRequest : IValidatableObject
{
    /// <summary>跨語系維持穩定且唯一的食物代碼。</summary>
    [Required(ErrorMessage = FoodMaintenanceErrorCodes.FoodCodeRequired)]
    [MaxLength(FoodMaintenanceRules.MaximumFoodCodeLength)]
    public string FoodCode { get; init; } = string.Empty;

    /// <summary>至少一筆且語系不可重複的食物翻譯。</summary>
    [MinLength(1, ErrorMessage = FoodMaintenanceErrorCodes.TranslationRequired)]
    public IReadOnlyList<UpsertFoodTranslationRequest> Translations { get; init; } = [];

    /// <summary>每 100 克營養素資料，營養素代碼不可重複。</summary>
    public IReadOnlyList<UpsertFoodNutrientRequest> Nutrients { get; init; } = [];

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Translations.Count == 0)
        {
            yield return new ValidationResult(
                FoodMaintenanceErrorCodes.TranslationRequired,
                [nameof(Translations)]);
        }

        if (Translations
            .GroupBy(item => item.LangCode, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            yield return new ValidationResult(
                FoodMaintenanceErrorCodes.DuplicateLangCode,
                [nameof(Translations)]);
        }

        if (Nutrients
            .GroupBy(item => item.NutrientCode, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            yield return new ValidationResult(
                FoodMaintenanceErrorCodes.DuplicateNutrient,
                [nameof(Nutrients)]);
        }
    }
}
