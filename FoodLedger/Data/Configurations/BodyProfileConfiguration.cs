using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

/// <summary>設定身體資料的欄位限制、使用者關聯與樂觀並行欄位。</summary>
internal sealed class BodyProfileConfiguration : IEntityTypeConfiguration<BodyProfile>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BodyProfile> entity)
    {
        entity.HasKey(profile => profile.UserId);
        entity.Property(profile => profile.HeightInCentimeters).HasPrecision(5, 2);
        entity.Property(profile => profile.Version).IsConcurrencyToken();

        entity.HasOne(profile => profile.User)
            .WithOne()
            .HasForeignKey<BodyProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.ToTable("body_profile", table =>
        {
            table.HasCheckConstraint(
                "ck_body_profile_height",
                "height_in_centimeters >= 100 AND height_in_centimeters <= 250");
        });

        entity.ConfigureBaseEntity();
    }
}
