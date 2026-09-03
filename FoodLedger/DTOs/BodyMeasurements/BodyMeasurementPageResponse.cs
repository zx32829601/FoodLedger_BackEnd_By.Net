namespace FoodLedger.DTOs.BodyMeasurements;

/// <summary>身體測量歷史的分頁回應。</summary>
public sealed class BodyMeasurementPageResponse
{
    public IReadOnlyList<BodyMeasurementResponse> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}
