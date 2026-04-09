using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities;

public abstract class BaseEntity
{
    [Column("created_at")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTimeOffset CreatedAt { get; set; }

    [MaxLength(200)] // 對應你資料庫的 varchar(200)
    [Column("created_by")]
    public string CreatedBy { get; set; } = "System";

    [Column("modified_at")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTimeOffset ModifiedAt { get; set; }

    [MaxLength(200)]
    [Column("modified_by")]
    public string? ModifiedBy { get; set; }
}