using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodLedger.Migrations
{
    /// <inheritdoc />
    public partial class AddNutrientDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "display_order",
                table: "nutrient",
                type: "integer",
                nullable: false,
                defaultValue: 1000);

            migrationBuilder.Sql("""
                UPDATE nutrient
                SET display_order = CASE nutrient_code
                    WHEN 'Calories' THEN 10 WHEN 'Protein' THEN 20
                    WHEN 'Carbohydrates' THEN 30 WHEN 'Fat' THEN 40
                    WHEN 'Sodium' THEN 50 WHEN 'SaturatedFat' THEN 60
                    WHEN 'DietaryFiber' THEN 70 WHEN 'Sugar' THEN 80
                    WHEN 'Cholesterol' THEN 90 WHEN 'Potassium' THEN 100
                    WHEN 'Calcium' THEN 110 WHEN 'Iron' THEN 120
                    WHEN 'VitaminA' THEN 130 WHEN 'VitaminC' THEN 140
                    ELSE 1000 END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "display_order",
                table: "nutrient");
        }
    }
}
