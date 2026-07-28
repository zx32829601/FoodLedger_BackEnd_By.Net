using System.Text.RegularExpressions;

namespace FoodLedger.Models;

/// <summary>
/// 定義 API 與翻譯資料共用的語系代碼限制。
/// </summary>
public static partial class LocalizationRules
{
    /// <summary>
    /// BCP 47 語系代碼允許的最大長度。
    /// </summary>
    public const int MaximumLangCodeLength = 255;

    /// <summary>
    /// 未指定語系時使用的預設語系。
    /// </summary>
    public const string DefaultLangCode = "zh-TW";

    /// <summary>
    /// 指定語系沒有翻譯時使用的英文 fallback。
    /// </summary>
    public const string FallbackLangCode = "en-US";

    /// <summary>
    /// 將通過驗證的語系代碼正規化為查詢比較格式。
    /// </summary>
    public static string NormalizeLangCode(string langCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(langCode);
        return langCode.ToLowerInvariant();
    }

    /// <summary>
    /// 驗證語系代碼是否符合本系統接受的 BCP 47 格式。
    /// </summary>
    public static bool IsValidLangCode(string? langCode)
    {
        if (string.IsNullOrWhiteSpace(langCode)
            || langCode.Length > MaximumLangCodeLength)
        {
            return false;
        }

        return LangCodePattern().IsMatch(langCode);
    }

    /// <summary>
    /// 驗證時區識別碼是否能由目前執行環境解析。
    /// </summary>
    public static bool IsValidTimeZone(string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
        {
            return false;
        }

        try
        {
            if (!TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZone, out _))
            {
                return false;
            }

            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    [GeneratedRegex(
        @"^(?:[A-Za-z]{2,8}(?:-[A-Za-z0-9]{1,8})*|[xXiI](?:-[A-Za-z0-9]{1,8})+)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex LangCodePattern();
}
