using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

internal sealed class NutrientConfiguration : IEntityTypeConfiguration<Nutrient>
{
    public void Configure(EntityTypeBuilder<Nutrient> entity)
    {
        entity.HasKey(e => e.NutrientId);

        entity.Property(e => e.NutrientId)
            .UseIdentityByDefaultColumn();

        entity.HasIndex(e => e.NutrientCode)
            .IsUnique();

        entity.ConfigureBaseEntity();
    }
}
