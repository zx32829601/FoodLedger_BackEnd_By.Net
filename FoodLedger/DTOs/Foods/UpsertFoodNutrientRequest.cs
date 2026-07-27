using System.ComponentModel.DataAnnotations;

namespace FoodLedger.DTOs.Foods;

/// <summary>
/// 建立或修改食物時提供的每 100 克營養素數值。
/// </summary>
public sealed class UpsertFoodNutrientRequest
{
    /// <summary>必須存在於 Nutrient 的穩定代碼。</summary>
    [Required]
    [MaxLength(FoodMaintenanceRules.MaximumFoodCodeLength)]
    public string NutrientCode { get; init; } = string.Empty;

    /// <summary>食物每 100 克所含的營養素數值。</summary>
    [Range(typeof(decimal), "0", FoodMaintenanceRules.MaximumNutrientAmount)]
    public decimal AmountPer100Grams { get; init; }
}
