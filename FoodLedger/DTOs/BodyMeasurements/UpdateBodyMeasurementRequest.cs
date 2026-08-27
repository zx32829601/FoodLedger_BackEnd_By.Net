using System.ComponentModel.DataAnnotations;
using FoodLedger.DTOs.Errors;

namespace FoodLedger.DTOs.BodyMeasurements;

/// <summary>修正身體測量值所需的輸入與最後讀取版本。</summary>
public sealed class UpdateBodyMeasurementRequest
{
    public decimal WeightInKilograms { get; init; }
    public decimal? BodyFatPercentage { get; init; }
    public decimal? MuscleMassInKilograms { get; init; }

    [Required(ErrorMessage = BodyMeasurementErrorCodes.VersionRequired)]
    public Guid? Version { get; init; }
}
