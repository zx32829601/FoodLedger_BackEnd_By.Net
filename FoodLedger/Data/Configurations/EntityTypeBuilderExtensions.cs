using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

/// <summary>提供領域實體共用的 EF Core 稽核欄位設定。</summary>
internal static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// 套用 <see cref="BaseEntity" /> 的時間、異動者與欄位長度設定。
    /// </summary>
    /// <typeparam name="T">繼承共用稽核欄位的實體型別。</typeparam>
    /// <param name="builder">要設定的 EF Core 實體建構器。</param>
    internal static void ConfigureBaseEntity<T>(this EntityTypeBuilder<T> builder)
        where T : BaseEntity
    {
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(200)
            .HasDefaultValue("System");

        builder.Property(e => e.ModifiedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(200);
    }
}
