namespace FoodLedger.DTOs.BodyMeasurements;

/// <summary>刪除身體測量前由後端計算並簽署的影響預覽。</summary>
public sealed class BodyMeasurementDeletionImpactResponse
{
    public long MeasurementId { get; init; }
    public Guid Version { get; init; }
    public int AffectedSnapshotCount { get; init; }
    public bool AffectsCurrentTarget { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public required string ImpactToken { get; init; }
}
