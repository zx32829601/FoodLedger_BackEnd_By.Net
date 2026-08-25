using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

/// <summary>設定食物主資料的主鍵、唯一代碼與稽核欄位。</summary>
internal sealed class SimpleFoodConfiguration : IEntityTypeConfiguration<SimpleFood>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SimpleFood> entity)
    {
        entity.HasKey(e => e.FoodId);

        entity.Property(e => e.FoodId)
            .UseIdentityByDefaultColumn();

        entity.HasIndex(e => e.FoodCode)
            .IsUnique();

        entity.ConfigureBaseEntity();
    }
}
