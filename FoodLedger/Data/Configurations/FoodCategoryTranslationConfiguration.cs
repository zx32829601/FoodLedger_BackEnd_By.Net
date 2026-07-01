using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

internal sealed class FoodCategoryTranslationConfiguration : IEntityTypeConfiguration<FoodCategoryTranslation>
{
    public void Configure(EntityTypeBuilder<FoodCategoryTranslation> entity)
    {
        entity.HasKey(e => e.TranslationId);

        entity.Property(e => e.TranslationId)
            .UseIdentityByDefaultColumn();

        entity.HasIndex(e => new { e.CategoryId, e.LangCode })
            .IsUnique()
            .HasDatabaseName("idx_category_translation_category_id_lang_code");

        entity.HasOne(e => e.FoodCategory)
            .WithMany(e => e.Translations)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.ConfigureBaseEntity();
    }
}
