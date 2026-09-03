namespace FoodLedger.DTOs.BodyMeasurements;

/// <summary>單筆身體測量的公開回應。</summary>
public sealed class BodyMeasurementResponse
{
    public long MeasurementId { get; init; }
    public decimal WeightInKilograms { get; init; }
    public decimal? BodyFatPercentage { get; init; }
    public decimal? MuscleMassInKilograms { get; init; }
    public DateTimeOffset MeasuredAt { get; init; }
    public Guid Version { get; init; }
}
