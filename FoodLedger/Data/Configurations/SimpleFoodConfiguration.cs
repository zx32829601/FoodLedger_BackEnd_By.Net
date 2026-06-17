using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

internal sealed class SimpleFoodConfiguration : IEntityTypeConfiguration<SimpleFood>
{
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
