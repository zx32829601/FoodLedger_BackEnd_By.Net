using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities;

/// <summary>
/// 表示可供跨功能共用的固定代碼。
/// </summary>
[Table("defined_code")]
public sealed class DefinedCode : BaseEntity
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
    /// 同類型代碼的顯示順序。
    /// </summary>
    [Column("sort_order")]
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否允許新資料使用此代碼。
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; }

    /// <summary>
    /// 各語系的顯示名稱與使用者說明。
    /// </summary>
    public ICollection<DefinedCodeTranslation> Translations { get; set; } = [];
}
