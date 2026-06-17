using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

internal sealed class FoodCategoryConfiguration : IEntityTypeConfiguration<FoodCategory>
{
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
