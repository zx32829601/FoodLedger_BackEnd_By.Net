using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

/// <summary>設定食物翻譯的欄位限制、唯一性與食物關聯。</summary>
internal sealed class SimpleFoodTranslationConfiguration : IEntityTypeConfiguration<SimpleFoodTranslation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SimpleFoodTranslation> entity)
    {
        entity.HasKey(e => e.TranslationId);

        entity.Property(e => e.TranslationId)
            .UseIdentityByDefaultColumn();

        entity.HasIndex(e => new { e.FoodId, e.LangCode })
            .IsUnique()
            .HasDatabaseName("ix_food_translation_food_id_lang_code");

        entity.HasOne(e => e.Food)
            .WithMany(e => e.Translations)
            .HasForeignKey(e => e.FoodId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.ConfigureBaseEntity();
    }
}
