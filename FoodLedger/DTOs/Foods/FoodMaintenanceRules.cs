namespace FoodLedger.DTOs.Foods;

/// <summary>
/// 集中管理食物維護 request 的欄位限制。
/// </summary>
public static class FoodMaintenanceRules
{
    public const int MaximumFoodCodeLength = 50;
    public const int MaximumDisplayNameLength = 200;
    public const string MaximumNutrientAmount = "99999999";
}
