using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

internal sealed class SimpleFoodCategoryConfiguration : IEntityTypeConfiguration<SimpleFoodCategory>
{
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
