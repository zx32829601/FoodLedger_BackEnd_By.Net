namespace FoodLedger.DTOs.Foods;

/// <summary>食物明細頁使用的本地化食物、分類與每 100 克營養資料。</summary>
public sealed class FoodDetailResponse
{
    /// <summary>食物識別碼。</summary>
    public long FoodId { get; init; }
    /// <summary>食物穩定代碼。</summary>
    public required string FoodCode { get; init; }
    /// <summary>依指定語系 fallback 後的顯示名稱。</summary>
    public required string DisplayName { get; init; }
    /// <summary>顯示名稱實際採用的語系。</summary>
    public required string LangCode { get; init; }
    /// <summary>主名稱非英文且不同時使用的英文副名稱。</summary>
    public string? EnglishName { get; init; }
    /// <summary>實際採用食物翻譯中的選填說明。</summary>
    public string? Description { get; init; }
    /// <summary>具有可用翻譯的分類。</summary>
    public IReadOnlyList<FoodCategoryResponse> Categories { get; init; } = [];
    /// <summary>依全域順位排列的完整營養素。</summary>
    public IReadOnlyList<FoodNutrientResponse> Nutrients { get; init; } = [];
}

/// <summary>食物明細中的一筆本地化分類。</summary>
public sealed class FoodCategoryResponse
{
    /// <summary>分類識別碼。</summary>
    public long CategoryId { get; init; }
    /// <summary>分類穩定代碼。</summary>
    public required string Code { get; init; }
    /// <summary>依指定語系 fallback 後的顯示名稱。</summary>
    public required string DisplayName { get; init; }
    /// <summary>分類顯示名稱實際採用的語系。</summary>
    public required string LangCode { get; init; }
}
