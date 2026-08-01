using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoodLedger.Models;

namespace FoodLedger.Data.Entities;

/// <summary>
/// 表示通用代碼的在地化顯示名稱與使用者說明。
/// </summary>
[Table("defined_code_translation")]
public sealed class DefinedCodeTranslation : BaseEntity
{
    /// <summary>
    /// 代碼所屬類型。
    /// </summary>
    [MaxLength(50)]
    [Column("code_type")]
    public required string CodeType { get; set; }

    /// <summary>
    /// 供 API 與資料紀錄保存的穩定代碼。
    /// </summary>
    [MaxLength(50)]
    [Column("code")]
    public required string Code { get; set; }

    /// <summary>
    /// BCP 47 語系代碼。
    /// </summary>
    [MaxLength(LocalizationRules.MaximumLangCodeLength)]
    [Column("lang_code")]
    public required string LangCode { get; set; }

    /// <summary>
    /// 指定語系的顯示名稱。
    /// </summary>
    [MaxLength(100)]
    [Column("display_name")]
    public required string DisplayName { get; set; }

    /// <summary>
    /// 提供使用者理解選項用途的在地化說明。
    /// </summary>
    [MaxLength(500)]
    [Column("note")]
    public string? Note { get; set; }

    /// <summary>
    /// 此翻譯所屬的通用代碼。
    /// </summary>
    public DefinedCode DefinedCode { get; set; } = default!;
}
