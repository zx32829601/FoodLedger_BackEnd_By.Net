namespace FoodLedger.DTOs.BodyMeasurements;

/// <summary>集中管理 Body Measurement API 的分頁與數值界線。</summary>
public static class BodyMeasurementRules
{
    public const int DefaultPageSize = 20;
    public const int MinimumPage = 1;
    public const int MinimumPageSize = 1;
    public const int MaximumPageSize = 100;
    public const decimal MinimumWeight = 20m;
    public const decimal MaximumWeight = 400m;
    public const decimal MinimumBodyFatPercentage = 2m;
    public const decimal MaximumBodyFatPercentage = 70m;
    public const int MaximumDecimalPlaces = 2;
}
