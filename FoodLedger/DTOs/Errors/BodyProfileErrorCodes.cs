namespace FoodLedger.DTOs.Errors;

/// <summary>
/// 身體資料 API 使用的穩定錯誤代碼。
/// </summary>
public static class BodyProfileErrorCodes
{
    public const string NotFound = "BodyProfile.NotFound";
    public const string Conflict = "BodyProfile.Conflict";
    public const string BirthDateRequired = "BodyProfile.BirthDateRequired";
    public const string AgeOutOfRange = "BodyProfile.AgeOutOfRange";
    public const string InvalidBiologicalSex = "BodyProfile.InvalidBiologicalSex";
    public const string HeightOutOfRange = "BodyProfile.HeightOutOfRange";
    public const string InvalidFitnessGoal = "BodyProfile.InvalidFitnessGoal";
    public const string InvalidActivityLevel = "BodyProfile.InvalidActivityLevel";
    public const string InvalidTimeZone = "BodyProfile.InvalidTimeZone";
}
