namespace FoodLedger.DTOs.Foods;

/// <summary>
/// 食物每 100 克所含的單一營養素。
/// </summary>
public sealed class FoodNutrientResponse
{
    /// <summary>
    /// 營養素穩定代碼。
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// 營養素顯示名稱。
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// 每 100 克食物的營養素含量。
    /// </summary>
    public decimal AmountPer100Grams { get; init; }

    /// <summary>
    /// 營養素計量單位代碼。
    /// </summary>
    public required string UnitCode { get; init; }
}
