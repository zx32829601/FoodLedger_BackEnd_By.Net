namespace FoodLedger.DTOs.Errors;

/// <summary>Body Measurement API 使用的穩定錯誤代碼。</summary>
public static class BodyMeasurementErrorCodes
{
    public const string NotFound = "BodyMeasurement.NotFound";
    public const string Conflict = "BodyMeasurement.Conflict";
    public const string ProfileRequired = "BodyMeasurement.ProfileRequired";
    public const string WeightOutOfRange = "BodyMeasurement.WeightOutOfRange";
    public const string BodyFatOutOfRange = "BodyMeasurement.BodyFatOutOfRange";
    public const string MuscleMassOutOfRange = "BodyMeasurement.MuscleMassOutOfRange";
    public const string PrecisionExceeded = "BodyMeasurement.PrecisionExceeded";
    public const string PageOutOfRange = "BodyMeasurement.PageOutOfRange";
    public const string PageSizeOutOfRange = "BodyMeasurement.PageSizeOutOfRange";
    public const string InvalidDateRange = "BodyMeasurement.InvalidDateRange";
    public const string VersionRequired = "BodyMeasurement.VersionRequired";
    public const string ImpactTokenRequired = "BodyMeasurement.ImpactTokenRequired";
}
