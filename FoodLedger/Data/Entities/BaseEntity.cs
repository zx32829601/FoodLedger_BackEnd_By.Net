using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities;

/// <summary>提供領域實體共用的建立與修改稽核欄位。</summary>
public abstract class BaseEntity
{
    /// <summary>取得或設定資料建立時間；一律以 UTC offset 儲存。</summary>
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>取得或設定建立資料的使用者識別名稱。</summary>
    [MaxLength(200)]
    [Column("created_by")]
    public string CreatedBy { get; set; } = "System";

    /// <summary>取得或設定資料最後修改時間；一律以 UTC offset 儲存。</summary>
    [Column("modified_at")]
    public DateTimeOffset ModifiedAt { get; set; }

    /// <summary>取得或設定最後修改資料的使用者識別名稱。</summary>
    [MaxLength(200)]
    [Column("modified_by")]
    public string? ModifiedBy { get; set; }
}
