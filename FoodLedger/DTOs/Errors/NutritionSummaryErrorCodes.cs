namespace FoodLedger.DTOs.Errors;

/// <summary>
/// Nutrition Summary 查詢參數驗證錯誤代碼。
/// </summary>
public static class NutritionSummaryErrorCodes
{
    /// <summary>缺少摘要日期。</summary>
    public const string DateRequired = "NutritionSummary.DateRequired";

    /// <summary>時區不是可解析的 IANA timezone。</summary>
    public const string InvalidTimeZone = "NutritionSummary.InvalidTimeZone";

    /// <summary>語系代碼不符合 BCP 47 格式。</summary>
    public const string InvalidLangCode = "NutritionSummary.InvalidLangCode";
}
