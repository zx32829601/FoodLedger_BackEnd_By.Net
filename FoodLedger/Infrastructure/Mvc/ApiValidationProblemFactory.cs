using System.Text.Json;
using FoodLedger.DTOs.Errors;
using FoodLedger.DTOs.Foods;
using Microsoft.AspNetCore.Mvc;

namespace FoodLedger.Infrastructure.Mvc;

/// <summary>
/// 將 ASP.NET Core ModelState 轉換成 FoodLedger code-first 驗證錯誤格式。
/// </summary>
public static class ApiValidationProblemFactory
{
    /// <summary>
    /// 建立包含 lower camel case 欄位、穩定錯誤代碼與 traceId 的 400 回應。
    /// </summary>
    /// <param name="context">目前 API action 與無效 ModelState 的執行內容。</param>
    /// <returns>符合 FoodLedger 契約的 <c>400 Bad Request</c>。</returns>
    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => ToCamelCase(entry.Key),
                entry => (IReadOnlyList<ApiFieldError>)entry.Value!.Errors
                    .Select(error => ToFieldError(error.ErrorMessage))
                    .DistinctBy(error => error.Code)
                    .ToArray());

        return new BadRequestObjectResult(new ApiErrorResponse
        {
            Code = ApiValidationErrorCodes.ValidationFailed,
            Message = "請確認輸入資料是否正確。",
            TraceId = context.HttpContext.TraceIdentifier,
            Errors = errors,
        });
    }

    /// <summary>
    /// 將 Service 回報的單一欄位驗證失敗轉換成相同的 code-first 400 契約。
    /// </summary>
    /// <param name="httpContext">目前 HTTP request context。</param>
    /// <param name="fieldName">發生錯誤的 request 欄位名稱。</param>
    /// <param name="errorCode">描述欄位規則的穩定錯誤代碼。</param>
    /// <returns>包含 lower camel case 欄位與 traceId 的 <c>400 Bad Request</c>。</returns>
    public static BadRequestObjectResult CreateForField(
        HttpContext httpContext,
        string fieldName,
        string errorCode)
    {
        return new BadRequestObjectResult(new ApiErrorResponse
        {
            Code = ApiValidationErrorCodes.ValidationFailed,
            Message = "請確認輸入資料是否正確。",
            TraceId = httpContext.TraceIdentifier,
            Errors = new Dictionary<string, IReadOnlyList<ApiFieldError>>
            {
                [ToCamelCase(fieldName)] = [ToFieldError(errorCode)],
            },
        });
    }

    private static ApiFieldError ToFieldError(string errorMessage)
    {
        var code = errorMessage.StartsWith("Auth.", StringComparison.Ordinal)
            || errorMessage.StartsWith("DailyRecord.", StringComparison.Ordinal)
            || errorMessage.StartsWith("BodyProfile.", StringComparison.Ordinal)
            || errorMessage.StartsWith("DefinedCode.", StringComparison.Ordinal)
            || errorMessage.StartsWith("FoodSearch.", StringComparison.Ordinal)
            || errorMessage.StartsWith("FoodMaintenance.", StringComparison.Ordinal)
            || errorMessage.StartsWith("NutritionSummary.", StringComparison.Ordinal)
            || errorMessage.StartsWith("Validation.", StringComparison.Ordinal)
                ? errorMessage
                : ApiValidationErrorCodes.InvalidValue;

        return new ApiFieldError
        {
            Code = code,
            Message = code switch
            {
                NutritionSummaryErrorCodes.DateRequired => "請提供摘要日期。",
                NutritionSummaryErrorCodes.InvalidTimeZone => "時區代碼不存在或無效。",
                NutritionSummaryErrorCodes.InvalidLangCode => "語系代碼格式不正確。",
                DefinedCodeErrorCodes.InvalidLangCode => "語系代碼格式不正確。",
                BodyProfileErrorCodes.BirthDateRequired => "請提供出生日期。",
                BodyProfileErrorCodes.AgeOutOfRange => "年齡必須介於 18 到 120 歲。",
                BodyProfileErrorCodes.InvalidBiologicalSex => "生理性別代碼不正確。",
                BodyProfileErrorCodes.HeightOutOfRange => "身高必須介於 100 到 250 公分。",
                BodyProfileErrorCodes.InvalidFitnessGoal => "健身目標代碼不存在或已停用。",
                BodyProfileErrorCodes.InvalidActivityLevel => "活動程度代碼不存在或已停用。",
                BodyProfileErrorCodes.InvalidTimeZone => "時區代碼不存在或無效。",
                ApiValidationErrorCodes.UserAccountInvalid =>
                    "使用者帳號須為 4 到 30 個英文字母、數字、底線或連字號。",
                ApiValidationErrorCodes.DisplayNameInvalid =>
                    "顯示名稱須為 1 到 30 個字元，且不可全為空白。",
                ApiValidationErrorCodes.EmailInvalid => "請輸入有效的電子郵件。",
                ApiValidationErrorCodes.PasswordInvalid =>
                    "密碼至少需要 8 個字元，並包含英文大小寫與數字。",
                DailyRecordErrorCodes.FoodIdInvalid =>
                    "食物識別碼必須大於 0。",
                DailyRecordErrorCodes.QuantityMustBeGreaterThanZero =>
                    "數量必須大於 0。",
                DailyRecordErrorCodes.QuantityOutOfRange =>
                    "數量必須介於 0.001 到 10000 之間。",
                DailyRecordErrorCodes.ConsumedAtCannotBeFuture =>
                    "食用時間不可晚於目前時間。",
                FoodSearchErrorCodes.InvalidLangCode => "語系代碼格式不正確。",
                FoodSearchErrorCodes.PageOutOfRange => "頁碼必須大於或等於 1。",
                FoodSearchErrorCodes.PageSizeOutOfRange =>
                    "每頁筆數必須介於 1 到 100 之間。",
                _ => "欄位值格式不正確。",
            },
            Parameters = code switch
            {
                DailyRecordErrorCodes.FoodIdInvalid =>
                    new Dictionary<string, object?> { ["min"] = 1 },
                DailyRecordErrorCodes.QuantityMustBeGreaterThanZero =>
                    new Dictionary<string, object?> { ["min"] = 0 },
                DailyRecordErrorCodes.QuantityOutOfRange =>
                    new Dictionary<string, object?>
                    {
                        ["min"] = 0.001m,
                        ["max"] = 10000m,
                    },
                BodyProfileErrorCodes.AgeOutOfRange =>
                    new Dictionary<string, object?> { ["min"] = 18, ["max"] = 120 },
                BodyProfileErrorCodes.HeightOutOfRange =>
                    new Dictionary<string, object?> { ["min"] = 100, ["max"] = 250 },
                FoodSearchErrorCodes.PageOutOfRange =>
                    new Dictionary<string, object?>
                    {
                        ["min"] = FoodSearchRequest.MinimumPage,
                    },
                FoodSearchErrorCodes.PageSizeOutOfRange =>
                    new Dictionary<string, object?>
                    {
                        ["min"] = FoodSearchRequest.MinimumPageSize,
                        ["max"] = FoodSearchRequest.MaximumPageSize,
                    },
                _ => null,
            },
        };
    }

    private static string ToCamelCase(string fieldName)
    {
        return string.IsNullOrEmpty(fieldName)
            ? fieldName
            : JsonNamingPolicy.CamelCase.ConvertName(fieldName);
    }
}
