using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLedger.Data.Entities;

public abstract class BaseEntity
{
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [MaxLength(200)]
    [Column("created_by")]
    public string CreatedBy { get; set; } = "System";

    [Column("modified_at")]
    public DateTimeOffset ModifiedAt { get; set; }

    [MaxLength(200)]
    [Column("modified_by")]
    public string? ModifiedBy { get; set; }
}
