using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodLedger.Migrations
{
    /// <inheritdoc />
    public partial class AddNutrientUnitCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "lang_code",
                table: "simple_food_translation",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "lang_code",
                table: "nutrient_translation",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AddColumn<string>(
                name: "unit_code",
                table: "nutrient",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE nutrient
                SET unit_code = CASE nutrient_code
                    WHEN 'Calories' THEN 'kcal'
                    WHEN 'Protein' THEN 'g'
                    WHEN 'Carbohydrates' THEN 'g'
                    WHEN 'Fat' THEN 'g'
                END
                WHERE nutrient_code IN ('Calories', 'Protein', 'Carbohydrates', 'Fat');

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM nutrient WHERE unit_code IS NULL) THEN
                        RAISE EXCEPTION
                            'Nutrient unit migration requires an explicit mapping for every existing nutrient_code.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "unit_code",
                table: "nutrient",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "g",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "lang_code",
                table: "food_category_translation",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "unit_code",
                table: "nutrient");

            migrationBuilder.AlterColumn<string>(
                name: "lang_code",
                table: "simple_food_translation",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "lang_code",
                table: "nutrient_translation",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "lang_code",
                table: "food_category_translation",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);
        }
    }
}
