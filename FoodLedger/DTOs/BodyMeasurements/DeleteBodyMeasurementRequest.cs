using System.ComponentModel.DataAnnotations;
using FoodLedger.DTOs.Errors;

namespace FoodLedger.DTOs.BodyMeasurements;

/// <summary>確認刪除身體測量時使用的版本與影響預覽 token。</summary>
public sealed class DeleteBodyMeasurementRequest
{
    [Required(ErrorMessage = BodyMeasurementErrorCodes.VersionRequired)]
    public Guid? Version { get; init; }

    [Required(ErrorMessage = BodyMeasurementErrorCodes.ImpactTokenRequired)]
    public string ImpactToken { get; init; } = string.Empty;
}
