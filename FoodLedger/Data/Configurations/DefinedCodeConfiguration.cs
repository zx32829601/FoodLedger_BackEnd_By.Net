using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

internal sealed class DefinedCodeConfiguration : IEntityTypeConfiguration<DefinedCode>
{
    private static readonly DateTimeOffset SeededAt =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<DefinedCode> entity)
    {
        entity.HasKey(code => new { code.CodeType, code.Code });
        entity.HasIndex(code => new { code.CodeType, code.IsActive, code.SortOrder })
            .HasDatabaseName("ix_defined_code_type_active_sort_order");

        entity.ConfigureBaseEntity();

        entity.HasData(
            CreateMealType("Breakfast", "早餐", 1),
            CreateMealType("Lunch", "午餐", 2),
            CreateMealType("Dinner", "晚餐", 3),
            CreateMealType("Snack", "點心", 4));
    }

    private static DefinedCode CreateMealType(string code, string displayName, int sortOrder)
    {
        return new DefinedCode
        {
            CodeType = DefinedCodeTypes.MealType,
            Code = code,
            DisplayName = displayName,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = SeededAt,
            CreatedBy = "Migration",
            ModifiedAt = SeededAt,
        };
    }
}
