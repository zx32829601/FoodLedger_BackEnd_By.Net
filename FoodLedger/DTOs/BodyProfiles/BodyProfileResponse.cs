namespace FoodLedger.DTOs.BodyProfiles;

/// <summary>
/// 目前使用者完整的身體資料。
/// </summary>
public sealed class BodyProfileResponse
{
    public DateOnly BirthDate { get; init; }
    public required string BiologicalSexCode { get; init; }
    public decimal HeightInCentimeters { get; init; }
    public required string FitnessGoalCode { get; init; }
    public required string ActivityLevelCode { get; init; }
    public required string TimeZone { get; init; }
    public Guid Version { get; init; }
}
