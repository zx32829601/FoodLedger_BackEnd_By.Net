using System.ComponentModel.DataAnnotations;
using FoodLedger.Models;

namespace FoodLedger.DTOs.Foods;

/// <summary>
/// 建立或修改食物時提供的一筆多語系內容。
/// </summary>
public sealed class UpsertFoodTranslationRequest
{
    /// <summary>BCP 47 語系代碼。</summary>
    [Required]
    [MaxLength(LocalizationRules.MaximumLangCodeLength)]
    public string LangCode { get; init; } = string.Empty;

    /// <summary>指定語系的食物顯示名稱。</summary>
    [Required]
    [MaxLength(FoodMaintenanceRules.MaximumDisplayNameLength)]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>指定語系的選填描述。</summary>
    public string? Description { get; init; }
}
