using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

internal sealed class NutrientTranslationConfiguration : IEntityTypeConfiguration<NutrientTranslation>
{
    public void Configure(EntityTypeBuilder<NutrientTranslation> entity)
    {
        entity.HasKey(e => e.TranslationId);

        entity.Property(e => e.TranslationId)
            .UseIdentityByDefaultColumn();

        entity.HasIndex(e => new { e.NutrientId, e.LangCode })
            .IsUnique()
            .HasDatabaseName("idx_nutrient_translation_nutrient_id_lang_code");

        entity.HasOne(e => e.Nutrient)
            .WithMany(e => e.Translations)
            .HasForeignKey(e => e.NutrientId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.ConfigureBaseEntity();
    }
}
