namespace FoodLedger.DTOs.Foods;

/// <summary>
/// 食物搜尋的分頁結果。
/// </summary>
public sealed class FoodSearchResponse
{
    /// <summary>
    /// 目前頁面的食物資料。
    /// </summary>
    public IReadOnlyList<FoodSearchItemResponse> Items { get; init; } = [];

    /// <summary>
    /// 目前頁碼。
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// 每頁筆數。
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// 符合搜尋條件的總筆數。
    /// </summary>
    public int TotalCount { get; init; }
}
