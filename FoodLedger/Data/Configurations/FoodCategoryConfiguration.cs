using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

/// <summary>設定食物分類的主鍵、代碼限制與稽核欄位。</summary>
internal sealed class FoodCategoryConfiguration : IEntityTypeConfiguration<FoodCategory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FoodCategory> entity)
    {
        entity.HasKey(e => e.CategoryId);

        entity.Property(e => e.CategoryId)
            .UseIdentityByDefaultColumn();

        entity.HasIndex(e => e.CategoryCode)
            .IsUnique();

        entity.ConfigureBaseEntity();
    }
}
