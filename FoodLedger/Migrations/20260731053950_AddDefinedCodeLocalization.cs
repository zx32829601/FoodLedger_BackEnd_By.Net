using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FoodLedger.Migrations
{
    /// <inheritdoc />
    public partial class AddDefinedCodeLocalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "defined_code_translation",
                columns: table => new
                {
                    code_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    lang_code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "System"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_defined_code_translation", x => new { x.code_type, x.code, x.lang_code });
                    table.ForeignKey(
                        name: "FK_defined_code_translation_defined_code_code_type_code",
                        columns: x => new { x.code_type, x.code },
                        principalTable: "defined_code",
                        principalColumns: new[] { "code_type", "code" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "defined_code",
                columns: new[] { "code", "code_type", "created_at", "created_by", "is_active", "modified_at", "modified_by", "sort_order" },
                values: new object[,]
                {
                    { "HIGH", "ACTIVITY_LEVEL", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 4 },
                    { "LIGHT", "ACTIVITY_LEVEL", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 2 },
                    { "MODERATE", "ACTIVITY_LEVEL", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 3 },
                    { "SEDENTARY", "ACTIVITY_LEVEL", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { "VERY_HIGH", "ACTIVITY_LEVEL", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5 },
                    { "FAT_LOSS", "FITNESS_GOAL", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { "MAINTAIN", "FITNESS_GOAL", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 2 },
                    { "MUSCLE_GAIN", "FITNESS_GOAL", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 3 }
                });

            migrationBuilder.InsertData(
                table: "defined_code_translation",
                columns: new[] { "code", "code_type", "lang_code", "created_at", "created_by", "display_name", "modified_at", "modified_by", "note" },
                values: new object[,]
                {
                    { "Breakfast", "MealType", "en-US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "Breakfast", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "The first meal of the day, typically eaten in the morning." },
                    { "Breakfast", "MealType", "zh-TW", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "早餐", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "通常於早晨或起床後食用的第一餐。" },
                    { "Dinner", "MealType", "en-US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "Dinner", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A meal typically eaten in the evening." },
                    { "Dinner", "MealType", "zh-TW", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "晚餐", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "通常於傍晚或晚間食用的正餐。" },
                    { "Lunch", "MealType", "en-US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "Lunch", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A meal typically eaten around midday." },
                    { "Lunch", "MealType", "zh-TW", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "午餐", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "通常於中午時段食用的正餐。" },
                    { "Snack", "MealType", "en-US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "Snack", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A smaller meal or food eaten between main meals." },
                    { "Snack", "MealType", "zh-TW", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "點心", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "在正餐之間食用的少量餐食。" },
                    { "HIGH", "ACTIVITY_LEVEL", "en-US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "Highly active", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Hard exercise or physical activity about six to seven days per week." },
                    { "HIGH", "ACTIVITY_LEVEL", "zh-TW", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "高度活動", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "每週約六至七天高強度運動或體力活動。" },
                    { "LIGHT", "ACTIVITY_LEVEL", "en-US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "Lightly active", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Light exercise or activity about one to three days per week." },
                    { "LIGHT", "ACTIVITY_LEVEL", "zh-TW", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "輕度活動", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "每週約一至三天輕度運動或日常活動量偏低。" },
                    { "MODERATE", "ACTIVITY_LEVEL", "en-US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "Moderately active", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Moderate exercise about three to five days per week." },
                    { "MODERATE", "ACTIVITY_LEVEL", "zh-TW", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "中度活動", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "每週約三至五天中等強度運動。" },
                    { "SEDENTARY", "ACTIVITY_LEVEL", "en-US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "Sedentary", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Mostly seated daily activity with little or no regular exercise." },
                    { "SEDENTARY", "ACTIVITY_LEVEL", "zh-TW", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "久坐", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "日常以坐姿活動為主，幾乎沒有規律運動。" },
                    { "VERY_HIGH", "ACTIVITY_LEVEL", "en-US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "Very highly active", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Very hard daily training or a physically demanding occupation." },
                    { "VERY_HIGH", "ACTIVITY_LEVEL", "zh-TW", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "極高活動", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "每日進行高強度訓練，或從事高度體力需求的工作。" },
                    { "FAT_LOSS", "FITNESS_GOAL", "en-US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "Fat loss", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Targets body fat reduction with calories set below estimated maintenance." },
                    { "FAT_LOSS", "FITNESS_GOAL", "zh-TW", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "減脂", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "以降低體脂為目標，建議熱量設定低於維持需求。" },
                    { "MAINTAIN", "FITNESS_GOAL", "en-US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "Maintain", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Targets maintaining current body weight and composition." },
                    { "MAINTAIN", "FITNESS_GOAL", "zh-TW", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "維持體重", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "以維持目前體重與身體組成為目標。" },
                    { "MUSCLE_GAIN", "FITNESS_GOAL", "en-US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "Muscle gain", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Targets muscle growth with calories set above estimated maintenance." },
                    { "MUSCLE_GAIN", "FITNESS_GOAL", "zh-TW", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Migration", "增肌", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "以增加肌肉量為目標，建議熱量設定高於維持需求。" }
                });

            migrationBuilder.Sql(
                """
                INSERT INTO defined_code_translation
                    (code_type, code, lang_code, display_name, note, created_at, created_by, modified_at, modified_by)
                SELECT
                    code_type,
                    code,
                    'zh-TW',
                    display_name,
                    NULL,
                    created_at,
                    created_by,
                    modified_at,
                    modified_by
                FROM defined_code
                ON CONFLICT (code_type, code, lang_code) DO NOTHING;
                """);

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "defined_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "defined_code",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE defined_code AS code
                SET display_name = COALESCE(
                    (
                        SELECT translation.display_name
                        FROM defined_code_translation AS translation
                        WHERE translation.code_type = code.code_type
                          AND translation.code = code.code
                        ORDER BY
                            CASE translation.lang_code
                                WHEN 'zh-TW' THEN 0
                                WHEN 'en-US' THEN 1
                                ELSE 2
                            END
                        LIMIT 1
                    ),
                    code.code);
                """);

            migrationBuilder.DropTable(
                name: "defined_code_translation");

            migrationBuilder.DeleteData(
                table: "defined_code",
                keyColumns: new[] { "code", "code_type" },
                keyValues: new object[] { "HIGH", "ACTIVITY_LEVEL" });

            migrationBuilder.DeleteData(
                table: "defined_code",
                keyColumns: new[] { "code", "code_type" },
                keyValues: new object[] { "LIGHT", "ACTIVITY_LEVEL" });

            migrationBuilder.DeleteData(
                table: "defined_code",
                keyColumns: new[] { "code", "code_type" },
                keyValues: new object[] { "MODERATE", "ACTIVITY_LEVEL" });

            migrationBuilder.DeleteData(
                table: "defined_code",
                keyColumns: new[] { "code", "code_type" },
                keyValues: new object[] { "SEDENTARY", "ACTIVITY_LEVEL" });

            migrationBuilder.DeleteData(
                table: "defined_code",
                keyColumns: new[] { "code", "code_type" },
                keyValues: new object[] { "VERY_HIGH", "ACTIVITY_LEVEL" });

            migrationBuilder.DeleteData(
                table: "defined_code",
                keyColumns: new[] { "code", "code_type" },
                keyValues: new object[] { "FAT_LOSS", "FITNESS_GOAL" });

            migrationBuilder.DeleteData(
                table: "defined_code",
                keyColumns: new[] { "code", "code_type" },
                keyValues: new object[] { "MAINTAIN", "FITNESS_GOAL" });

            migrationBuilder.DeleteData(
                table: "defined_code",
                keyColumns: new[] { "code", "code_type" },
                keyValues: new object[] { "MUSCLE_GAIN", "FITNESS_GOAL" });

        }
    }
}
