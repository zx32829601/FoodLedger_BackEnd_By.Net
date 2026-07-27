namespace FoodLedger.DTOs.Errors;

/// <summary>
/// 食物搜尋 API 與前端共同使用的穩定驗證錯誤代碼。
/// </summary>
public static class FoodSearchErrorCodes
{
    /// <summary>
    /// 搜尋文字為空白。
    /// </summary>
    public const string QueryRequired = "FoodSearch.QueryRequired";

    /// <summary>
    /// 語系代碼不是支援的 BCP 47 基本格式。
    /// </summary>
    public const string InvalidLangCode = "FoodSearch.InvalidLangCode";

    /// <summary>
    /// 頁碼小於 1。
    /// </summary>
    public const string PageOutOfRange = "FoodSearch.PageOutOfRange";

    /// <summary>
    /// 每頁筆數不在 1 到 100 之間。
    /// </summary>
    public const string PageSizeOutOfRange = "FoodSearch.PageSizeOutOfRange";
}
