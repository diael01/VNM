using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSettlementAndTransferFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mode",
                table: "ProviderSettlements");

            migrationBuilder.AddColumn<int>(
                name: "SettlementMode",
                table: "ProviderSettlements",
                type: "int",
                maxLength: 50,
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SettlementMode",
                table: "ProviderSettlements");

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "ProviderSettlements",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
