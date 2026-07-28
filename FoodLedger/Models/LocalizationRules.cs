namespace FoodLedger.Models;

/// <summary>
/// 定義 API 與翻譯資料共用的語系代碼限制。
/// </summary>
public static class LocalizationRules
{
    /// <summary>
    /// BCP 47 語系代碼允許的最大長度。
    /// </summary>
    public const int MaximumLangCodeLength = 255;
}
