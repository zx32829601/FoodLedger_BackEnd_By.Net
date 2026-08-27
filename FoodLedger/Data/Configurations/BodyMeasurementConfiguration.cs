using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

/// <summary>設定身體測量的精度、查詢索引、使用者關聯與並行版本。</summary>
internal sealed class BodyMeasurementConfiguration
    : IEntityTypeConfiguration<BodyMeasurement>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BodyMeasurement> entity)
    {
        entity.HasKey(measurement => measurement.MeasurementId);
        entity.Property(measurement => measurement.WeightInKilograms).HasPrecision(5, 2);
        entity.Property(measurement => measurement.BodyFatPercentage).HasPrecision(4, 2);
        entity.Property(measurement => measurement.MuscleMassInKilograms).HasPrecision(5, 2);
        entity.Property(measurement => measurement.Version).IsConcurrencyToken();

        entity.HasIndex(measurement => new
        {
            measurement.UserId,
            measurement.MeasuredAt,
            measurement.CreatedAt,
            measurement.MeasurementId,
        })
            .IsDescending(false, true, true, true)
            .HasDatabaseName("ix_body_measurement_user_history");

        entity.HasOne(measurement => measurement.User)
            .WithMany()
            .HasForeignKey(measurement => measurement.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.ToTable("body_measurement", table =>
        {
            table.HasCheckConstraint(
                "ck_body_measurement_weight",
                "weight_in_kilograms >= 20 AND weight_in_kilograms <= 400");
            table.HasCheckConstraint(
                "ck_body_measurement_body_fat",
                "body_fat_percentage IS NULL OR "
                + "(body_fat_percentage >= 2 AND body_fat_percentage <= 70)");
            table.HasCheckConstraint(
                "ck_body_measurement_muscle_mass",
                "muscle_mass_in_kilograms IS NULL OR "
                + "(muscle_mass_in_kilograms > 0 "
                + "AND muscle_mass_in_kilograms <= weight_in_kilograms)");
        });

        entity.ConfigureBaseEntity();
    }
}
