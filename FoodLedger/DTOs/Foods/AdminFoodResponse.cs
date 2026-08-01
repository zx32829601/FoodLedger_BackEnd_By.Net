namespace FoodLedger.DTOs.Foods;

/// <summary>
/// 管理員食物維護頁使用的完整食物資料。
/// </summary>
public sealed class AdminFoodResponse
{
    /// <summary>食物識別碼。</summary>
    public long FoodId { get; init; }
    /// <summary>食物穩定代碼。</summary>
    public string FoodCode { get; init; } = string.Empty;
    /// <summary>完整多語系內容。</summary>
    public IReadOnlyList<UpsertFoodTranslationRequest> Translations { get; init; } = [];
    /// <summary>完整每 100 克營養素資料。</summary>
    public IReadOnlyList<AdminFoodNutrientResponse> Nutrients { get; init; } = [];
}

/// <summary>
/// 管理員食物維護頁使用的營養素資料。
/// </summary>
public sealed class AdminFoodNutrientResponse
{
    /// <summary>營養素穩定代碼。</summary>
    public string NutrientCode { get; init; } = string.Empty;
    /// <summary>每 100 克含量。</summary>
    public decimal AmountPer100Grams { get; init; }
    /// <summary>營養素單位代碼。</summary>
    public string UnitCode { get; init; } = string.Empty;
    /// <summary>跨畫面一致的顯示順序。</summary>
    public int DisplayOrder { get; init; }
}
