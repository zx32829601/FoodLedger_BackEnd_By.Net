using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

/// <summary>設定營養素翻譯的欄位限制、唯一性與營養素關聯。</summary>
internal sealed class NutrientTranslationConfiguration : IEntityTypeConfiguration<NutrientTranslation>
{
    /// <inheritdoc />
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
