namespace FoodLedger.DTOs.DefinedCodes;

/// <summary>
/// 提供前端下拉選單所需的通用代碼資料。
/// </summary>
public sealed class DefinedCodeResponse
{
    /// <summary>
    /// 穩定代碼值。
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// 預設顯示名稱。
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// 實際採用翻譯的 BCP 47 語系代碼；找不到翻譯時為 <see langword="null" />。
    /// </summary>
    public string? LangCode { get; init; }

    /// <summary>
    /// 提供使用者理解選項用途的在地化說明；找不到翻譯時為 <see langword="null" />。
    /// </summary>
    public string? Note { get; init; }

    /// <summary>
    /// 顯示順序。
    /// </summary>
    public int SortOrder { get; init; }
}
