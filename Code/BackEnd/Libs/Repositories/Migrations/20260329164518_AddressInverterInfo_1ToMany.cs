using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddressInverterInfo_1ToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InverterId",
                table: "Addresses");

            migrationBuilder.AddColumn<int>(
                name: "AddressId",
                table: "InverterInfos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_InverterInfos_AddressId",
                table: "InverterInfos",
                column: "AddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_InverterInfos_Addresses_AddressId",
                table: "InverterInfos",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InverterInfos_Addresses_AddressId",
                table: "InverterInfos");

            migrationBuilder.DropIndex(
                name: "IX_InverterInfos_AddressId",
                table: "InverterInfos");

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "InverterInfos");

            migrationBuilder.AddColumn<int>(
                name: "InverterId",
                table: "Addresses",
                type: "int",
                nullable: true);
        }
    }
}
