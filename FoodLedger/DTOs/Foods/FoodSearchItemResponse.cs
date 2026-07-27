namespace FoodLedger.DTOs.Foods;

/// <summary>
/// 食物搜尋結果中的單筆食物資料。
/// </summary>
public sealed class FoodSearchItemResponse
{
    /// <summary>
    /// 食物識別碼。
    /// </summary>
    public long FoodId { get; init; }

    /// <summary>
    /// 食物穩定代碼。
    /// </summary>
    public required string FoodCode { get; init; }

    /// <summary>
    /// 實際採用翻譯的顯示名稱。
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// 實際採用的 BCP 47 語系代碼。
    /// </summary>
    public required string LangCode { get; init; }

    /// <summary>
    /// 每 100 克的動態營養素清單。
    /// </summary>
    public IReadOnlyList<FoodNutrientResponse> Nutrients { get; init; } = [];
}
