namespace FoodLedger.DTOs.Errors;

/// <summary>
/// 每日飲食紀錄 API 與前端共同使用的穩定錯誤代碼。
/// </summary>
public static class DailyRecordErrorCodes
{
    /// <summary>
    /// 指定的每日飲食紀錄不存在。
    /// </summary>
    public const string NotFound = "DailyRecord.NotFound";

    /// <summary>
    /// 建立飲食紀錄時找不到指定食物。
    /// </summary>
    public const string FoodNotFound = "DailyRecord.FoodNotFound";

    /// <summary>
    /// 食物識別碼不符合正整數限制。
    /// </summary>
    public const string FoodIdInvalid = "DailyRecord.FoodIdInvalid";

    /// <summary>
    /// 食用數量不是正數。
    /// </summary>
    public const string QuantityMustBeGreaterThanZero =
        "DailyRecord.QuantityMustBeGreaterThanZero";

    /// <summary>
    /// 食用數量超出系統允許的紀錄範圍。
    /// </summary>
    public const string QuantityOutOfRange = "DailyRecord.QuantityOutOfRange";

    /// <summary>
    /// 食用時間晚於伺服器目前 UTC 時間。
    /// </summary>
    public const string ConsumedAtCannotBeFuture =
        "DailyRecord.ConsumedAtCannotBeFuture";

    /// <summary>
    /// 餐別不存在、已停用或不是 MealType。
    /// </summary>
    public const string InvalidMealType = "DailyRecord.InvalidMealType";

    /// <summary>
    /// 備註 trim 後超過 500 字元。
    /// </summary>
    public const string NoteTooLong = "DailyRecord.NoteTooLong";
}
