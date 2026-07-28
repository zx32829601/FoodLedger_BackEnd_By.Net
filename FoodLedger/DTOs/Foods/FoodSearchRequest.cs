using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using FoodLedger.DTOs.Errors;
using FoodLedger.Models;

namespace FoodLedger.DTOs.Foods;

/// <summary>
/// 食物搜尋的查詢條件。
/// </summary>
public sealed partial class FoodSearchRequest : IValidatableObject
{
    /// <summary>
    /// 預設使用的繁體中文語系。
    /// </summary>
    public const string DefaultLangCode = "zh-TW";

    /// <summary>
    /// 指定語系缺少翻譯時使用的英文語系。
    /// </summary>
    public const string FallbackLangCode = "en-US";

    /// <summary>
    /// 預設每頁筆數。
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// 第一個有效頁碼。
    /// </summary>
    public const int MinimumPage = 1;

    /// <summary>
    /// 單次查詢允許的最小筆數。
    /// </summary>
    public const int MinimumPageSize = 1;

    /// <summary>
    /// 單次查詢允許的最大筆數。
    /// </summary>
    public const int MaximumPageSize = 100;

    /// <summary>
    /// 選填的食物名稱搜尋文字；省略或 trim 後為空白時不套用名稱篩選。
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// BCP 47 語系代碼。
    /// </summary>
    [Required(ErrorMessage = FoodSearchErrorCodes.InvalidLangCode)]
    public string LangCode { get; init; } = DefaultLangCode;

    /// <summary>
    /// 從 1 開始的頁碼。
    /// </summary>
    public int Page { get; init; } = MinimumPage;

    /// <summary>
    /// 每頁筆數，上限為 100。
    /// </summary>
    public int PageSize { get; init; } = DefaultPageSize;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsValidLangCode(LangCode))
        {
            yield return new ValidationResult(
                FoodSearchErrorCodes.InvalidLangCode,
                [nameof(LangCode)]);
        }

        if (Page < MinimumPage)
        {
            yield return new ValidationResult(
                FoodSearchErrorCodes.PageOutOfRange,
                [nameof(Page)]);
        }

        if (PageSize is < MinimumPageSize or > MaximumPageSize)
        {
            yield return new ValidationResult(
                FoodSearchErrorCodes.PageSizeOutOfRange,
                [nameof(PageSize)]);
        }
    }

    private static bool IsValidLangCode(string langCode)
    {
        if (string.IsNullOrWhiteSpace(langCode)
            || langCode.Length > LocalizationRules.MaximumLangCodeLength)
        {
            return false;
        }

        return LangCodePattern().IsMatch(langCode);
    }

    [GeneratedRegex(
        @"^(?:[A-Za-z]{2,8}(?:-[A-Za-z0-9]{1,8})*|[xXiI](?:-[A-Za-z0-9]{1,8})+)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex LangCodePattern();
}
