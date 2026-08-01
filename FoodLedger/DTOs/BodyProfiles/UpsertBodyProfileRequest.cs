using System.ComponentModel.DataAnnotations;
using FoodLedger.DTOs.Errors;

namespace FoodLedger.DTOs.BodyProfiles;

/// <summary>
/// 新增或修改目前使用者身體資料的輸入。
/// </summary>
public sealed class UpsertBodyProfileRequest
{
    [Required(ErrorMessage = BodyProfileErrorCodes.BirthDateRequired)]
    public DateOnly? BirthDate { get; set; }

    [Required(ErrorMessage = BodyProfileErrorCodes.InvalidBiologicalSex)]
    public required string BiologicalSexCode { get; set; }

    public decimal HeightInCentimeters { get; set; }

    [Required(ErrorMessage = BodyProfileErrorCodes.InvalidFitnessGoal)]
    public required string FitnessGoalCode { get; set; }

    [Required(ErrorMessage = BodyProfileErrorCodes.InvalidActivityLevel)]
    public required string ActivityLevelCode { get; set; }

    [Required(ErrorMessage = BodyProfileErrorCodes.InvalidTimeZone)]
    public required string TimeZone { get; set; }

    /// <summary>
    /// 建立時留空；修改時必須傳回最後讀取的版本。
    /// </summary>
    public Guid? Version { get; set; }
}
