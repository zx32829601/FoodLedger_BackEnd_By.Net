using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

/// <summary>設定營養素代碼、單位、顯示順序與唯一性限制。</summary>
internal sealed class NutrientConfiguration : IEntityTypeConfiguration<Nutrient>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Nutrient> entity)
    {
        entity.HasKey(e => e.NutrientId);

        entity.Property(e => e.NutrientId)
            .UseIdentityByDefaultColumn();

        entity.HasIndex(e => e.NutrientCode)
            .IsUnique();

        entity.Property(e => e.UnitCode)
            .HasDefaultValue(NutrientUnitCodes.Gram);

        entity.Property(e => e.DisplayOrder)
            .HasDefaultValue(1000);

        entity.ConfigureBaseEntity();
    }
}
