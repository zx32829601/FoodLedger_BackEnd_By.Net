using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodLedger.Migrations
{
    /// <inheritdoc />
    public partial class AddBodyProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "body_profile",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    biological_sex_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    height_in_centimeters = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    fitness_goal_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    activity_level_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    time_zone = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "System"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_body_profile", x => x.user_id);
                    table.CheckConstraint("ck_body_profile_height", "height_in_centimeters >= 100 AND height_in_centimeters <= 250");
                    table.ForeignKey(
                        name: "FK_body_profile_application_user_user_id",
                        column: x => x.user_id,
                        principalTable: "application_user",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "body_profile");
        }
    }
}
