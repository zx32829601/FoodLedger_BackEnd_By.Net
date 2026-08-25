using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

/// <summary>設定每日飲食紀錄的資料表、索引、欄位限制與關聯。</summary>
internal sealed class DailyRecordConfiguration : IEntityTypeConfiguration<DailyRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DailyRecord> entity)
    {
        entity.HasKey(e => e.RecordId);

        entity.Property(e => e.RecordId)
            .UseIdentityByDefaultColumn();

        entity.Property(e => e.Quantity)
            .HasPrecision(10, 3);

        entity.Property(e => e.MealTypeCode)
            .HasDefaultValue(MealTypeCodes.Snack);

        entity.HasIndex(e => new { e.UserId, e.ConsumedAt })
            .HasDatabaseName("ix_daily_record_user_id_consumed_at");

        entity.HasIndex(e => e.FoodId)
            .HasDatabaseName("ix_daily_record_food_id");

        entity.HasOne(e => e.Food)
            .WithMany()
            .HasForeignKey(e => e.FoodId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable("daily_record", table =>
        {
            table.HasCheckConstraint("ck_daily_record_quantity_positive", "quantity > 0");
        });

        entity.ConfigureBaseEntity();
    }
}
