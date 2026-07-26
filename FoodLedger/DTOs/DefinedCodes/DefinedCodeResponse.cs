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
    /// 顯示順序。
    /// </summary>
    public int SortOrder { get; init; }
}
