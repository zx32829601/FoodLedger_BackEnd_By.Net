using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

/// <summary>設定通用代碼的複合鍵、欄位限制與內建代碼種子資料。</summary>
internal sealed class DefinedCodeConfiguration : IEntityTypeConfiguration<DefinedCode>
{
    private static readonly DateTimeOffset SeededAt =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DefinedCode> entity)
    {
        entity.HasKey(code => new { code.CodeType, code.Code });
        entity.HasIndex(code => new { code.CodeType, code.IsActive, code.SortOrder })
            .HasDatabaseName("ix_defined_code_type_active_sort_order");

        entity.ConfigureBaseEntity();

        entity.HasData(
            CreateCode(DefinedCodeTypes.MealType, "Breakfast", 1),
            CreateCode(DefinedCodeTypes.MealType, "Lunch", 2),
            CreateCode(DefinedCodeTypes.MealType, "Dinner", 3),
            CreateCode(DefinedCodeTypes.MealType, "Snack", 4),
            CreateCode(DefinedCodeTypes.FitnessGoal, "FAT_LOSS", 1),
            CreateCode(DefinedCodeTypes.FitnessGoal, "MAINTAIN", 2),
            CreateCode(DefinedCodeTypes.FitnessGoal, "MUSCLE_GAIN", 3),
            CreateCode(DefinedCodeTypes.ActivityLevel, "SEDENTARY", 1),
            CreateCode(DefinedCodeTypes.ActivityLevel, "LIGHT", 2),
            CreateCode(DefinedCodeTypes.ActivityLevel, "MODERATE", 3),
            CreateCode(DefinedCodeTypes.ActivityLevel, "HIGH", 4),
            CreateCode(DefinedCodeTypes.ActivityLevel, "VERY_HIGH", 5));
    }

    private static DefinedCode CreateCode(string codeType, string code, int sortOrder)
    {
        return new DefinedCode
        {
            CodeType = codeType,
            Code = code,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = SeededAt,
            CreatedBy = "Migration",
            ModifiedAt = SeededAt,
        };
    }
}
