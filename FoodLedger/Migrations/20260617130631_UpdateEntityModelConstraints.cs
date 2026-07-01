using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FoodLedger.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntityModelConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_account");

            migrationBuilder.RenameIndex(
                name: "idx_category_translation_nutrient_id_lang_code",
                table: "nutrient_translation",
                newName: "idx_nutrient_translation_nutrient_id_lang_code");

            migrationBuilder.RenameColumn(
                name: "TranslationId",
                table: "food_category_translation",
                newName: "translation_id");

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "simple_food_translation",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "System",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "simple_food_category",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "System",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "food_code",
                table: "simple_food",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "simple_food",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "System",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "nutrient_translation",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "System",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "nutrient",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "System",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "per_unit",
                table: "food_nutrient",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "100",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "food_nutrient",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "System",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "food_nutrient",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "food_category_translation",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "System",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "food_category",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "System",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "daily_record",
                type: "numeric(10,3)",
                precision: 10,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "daily_record",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "System",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddCheckConstraint(
                name: "ck_food_nutrient_amount_non_negative",
                table: "food_nutrient",
                sql: "amount >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_daily_record_food_id",
                table: "daily_record",
                column: "food_id");

            migrationBuilder.CreateIndex(
                name: "ix_daily_record_user_id_consumed_at",
                table: "daily_record",
                columns: new[] { "user_id", "consumed_at" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_daily_record_quantity_positive",
                table: "daily_record",
                sql: "quantity > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_daily_record_simple_food_food_id",
                table: "daily_record",
                column: "food_id",
                principalTable: "simple_food",
                principalColumn: "food_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_daily_record_simple_food_food_id",
                table: "daily_record");

            migrationBuilder.DropCheckConstraint(
                name: "ck_food_nutrient_amount_non_negative",
                table: "food_nutrient");

            migrationBuilder.DropIndex(
                name: "ix_daily_record_food_id",
                table: "daily_record");

            migrationBuilder.DropIndex(
                name: "ix_daily_record_user_id_consumed_at",
                table: "daily_record");

            migrationBuilder.DropCheckConstraint(
                name: "ck_daily_record_quantity_positive",
                table: "daily_record");

            migrationBuilder.RenameIndex(
                name: "idx_nutrient_translation_nutrient_id_lang_code",
                table: "nutrient_translation",
                newName: "idx_category_translation_nutrient_id_lang_code");

            migrationBuilder.RenameColumn(
                name: "translation_id",
                table: "food_category_translation",
                newName: "TranslationId");

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "simple_food_translation",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldDefaultValue: "System");

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "simple_food_category",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldDefaultValue: "System");

            migrationBuilder.AlterColumn<string>(
                name: "food_code",
                table: "simple_food",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "simple_food",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldDefaultValue: "System");

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "nutrient_translation",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldDefaultValue: "System");

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "nutrient",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldDefaultValue: "System");

            migrationBuilder.AlterColumn<string>(
                name: "per_unit",
                table: "food_nutrient",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "100");

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "food_nutrient",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldDefaultValue: "System");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "food_nutrient",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)",
                oldPrecision: 12,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "food_category_translation",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldDefaultValue: "System");

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "food_category",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldDefaultValue: "System");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "daily_record",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,3)",
                oldPrecision: 10,
                oldScale: 3);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "daily_record",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldDefaultValue: "System");

            migrationBuilder.CreateTable(
                name: "user_account",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    account_status = table.Column<byte>(type: "smallint", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_account", x => x.user_id);
                });
        }
    }
}
