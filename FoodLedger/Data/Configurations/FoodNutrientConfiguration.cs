using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

internal sealed class FoodNutrientConfiguration : IEntityTypeConfiguration<FoodNutrient>
{
    public void Configure(EntityTypeBuilder<FoodNutrient> entity)
    {
        entity.HasKey(e => new { e.FoodId, e.NutrientId });

        entity.Property(e => e.Amount)
            .HasPrecision(12, 4);

        entity.Property(e => e.PerUnit)
            .HasMaxLength(20)
            .HasDefaultValue("100");

        entity.HasOne(e => e.Food)
            .WithMany()
            .HasForeignKey(e => e.FoodId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Nutrient)
            .WithMany()
            .HasForeignKey(e => e.NutrientId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.ToTable("food_nutrient", table =>
        {
            table.HasCheckConstraint("ck_food_nutrient_amount_non_negative", "amount >= 0");
        });

        entity.ConfigureBaseEntity();
    }
}
