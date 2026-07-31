namespace FoodLedger.Data.Entities;

/// <summary>
/// 定義通用代碼表目前支援的代碼類型。
/// </summary>
public static class DefinedCodeTypes
{
    /// <summary>
    /// 飲食紀錄的餐別代碼類型。
    /// </summary>
    public const string MealType = "MealType";

    /// <summary>
    /// 每日建議攝取量使用的健身目標代碼類型。
    /// </summary>
    public const string FitnessGoal = "FITNESS_GOAL";

    /// <summary>
    /// 每日建議攝取量使用的活動程度代碼類型。
    /// </summary>
    public const string ActivityLevel = "ACTIVITY_LEVEL";
}
