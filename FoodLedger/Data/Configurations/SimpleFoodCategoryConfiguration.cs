using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

/// <summary>設定食物與分類之間的多對多連接實體。</summary>
internal sealed class SimpleFoodCategoryConfiguration : IEntityTypeConfiguration<SimpleFoodCategory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SimpleFoodCategory> entity)
    {
        entity.HasKey(e => new { e.FoodId, e.CategoryId });

        entity.HasOne(e => e.Food)
            .WithMany()
            .HasForeignKey(e => e.FoodId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.ConfigureBaseEntity();
    }
}
