namespace FoodLedger.DTOs.BodyMeasurements;

/// <summary>建立身體測量所需的 client 輸入。</summary>
public sealed class CreateBodyMeasurementRequest
{
    public decimal WeightInKilograms { get; init; }
    public decimal? BodyFatPercentage { get; init; }
    public decimal? MuscleMassInKilograms { get; init; }
}
