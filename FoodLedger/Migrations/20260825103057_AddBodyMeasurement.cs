using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FoodLedger.Migrations
{
    /// <inheritdoc />
    public partial class AddBodyMeasurement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "body_measurement",
                columns: table => new
                {
                    measurement_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    weight_in_kilograms = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    body_fat_percentage = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    muscle_mass_in_kilograms = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    measured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "System"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_body_measurement", x => x.measurement_id);
                    table.CheckConstraint("ck_body_measurement_body_fat", "body_fat_percentage IS NULL OR (body_fat_percentage >= 2 AND body_fat_percentage <= 70)");
                    table.CheckConstraint("ck_body_measurement_muscle_mass", "muscle_mass_in_kilograms IS NULL OR (muscle_mass_in_kilograms > 0 AND muscle_mass_in_kilograms <= weight_in_kilograms)");
                    table.CheckConstraint("ck_body_measurement_weight", "weight_in_kilograms >= 20 AND weight_in_kilograms <= 400");
                    table.ForeignKey(
                        name: "FK_body_measurement_application_user_user_id",
                        column: x => x.user_id,
                        principalTable: "application_user",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_body_measurement_user_history",
                table: "body_measurement",
                columns: new[] { "user_id", "measured_at", "created_at", "measurement_id" },
                descending: new[] { false, true, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "body_measurement");
        }
    }
}
