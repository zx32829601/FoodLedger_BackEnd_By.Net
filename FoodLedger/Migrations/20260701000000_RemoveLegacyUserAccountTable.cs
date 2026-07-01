using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodLedger.Migrations
{
    /// <inheritdoc />
    [Migration("20260701000000_RemoveLegacyUserAccountTable")]
    public partial class RemoveLegacyUserAccountTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS user_account;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
