using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

/// <summary>設定食物分類翻譯的欄位限制、唯一性與分類關聯。</summary>
internal sealed class FoodCategoryTranslationConfiguration : IEntityTypeConfiguration<FoodCategoryTranslation>
{
    /// <inheritdoc />
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
