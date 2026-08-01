namespace FoodLedger.DTOs.Errors;

/// <summary>
/// DefinedCode API 對外使用的穩定錯誤代碼。
/// </summary>
public static class DefinedCodeErrorCodes
{
    /// <summary>
    /// 查詢語系不是本系統接受的 BCP 47 格式。
    /// </summary>
    public const string InvalidLangCode = "DefinedCode.InvalidLangCode";
}
