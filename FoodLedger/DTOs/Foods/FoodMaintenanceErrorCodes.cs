namespace FoodLedger.DTOs.Foods;

/// <summary>
/// 食物維護 API 使用的穩定錯誤代碼。
/// </summary>
public static class FoodMaintenanceErrorCodes
{
    public const string FoodCodeRequired = "FoodMaintenance.FoodCodeRequired";
    public const string TranslationRequired = "FoodMaintenance.TranslationRequired";
    public const string DuplicateLangCode = "FoodMaintenance.DuplicateLangCode";
    public const string DuplicateFoodCode = "FoodMaintenance.DuplicateFoodCode";
    public const string NutrientNotFound = "FoodMaintenance.NutrientNotFound";
    public const string DuplicateNutrient = "FoodMaintenance.DuplicateNutrient";
    public const string NotFound = "FoodMaintenance.NotFound";
    public const string InUse = "FoodMaintenance.InUse";
}
