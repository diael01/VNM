using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddressIdRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsumptionReadings_Addresses",
                table: "ConsumptionReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_InverterReadings_Addresses",
                table: "InverterReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_InverterReadings_InverterInfos",
                table: "InverterReadings");

            migrationBuilder.DropIndex(
                name: "IX_InverterReadings_InverterId",
                table: "InverterReadings");

            migrationBuilder.DropIndex(
                name: "IX_ConsumptionReadings_LocationId",
                table: "ConsumptionReadings");

            migrationBuilder.DropColumn(
                name: "InverterId",
                table: "InverterReadings");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "ConsumptionReadings");

            migrationBuilder.RenameColumn(
                name: "LocationId",
                table: "ProviderSettlements",
                newName: "AddressId");

            migrationBuilder.RenameIndex(
                name: "IX_ProviderSettlements_LocationId",
                table: "ProviderSettlements",
                newName: "IX_ProviderSettlements_AddressId");

            migrationBuilder.RenameColumn(
                name: "LocationId",
                table: "InverterReadings",
                newName: "InverterInfoId");

            migrationBuilder.RenameIndex(
                name: "IX_InverterReadings_LocationId",
                table: "InverterReadings",
                newName: "IX_InverterReadings_InverterInfoId");

            migrationBuilder.RenameColumn(
                name: "LocationId",
                table: "DailyEnergyBalances",
                newName: "AddressId");

            migrationBuilder.RenameIndex(
                name: "IX_DailyEnergyBalances_LocationId",
                table: "DailyEnergyBalances",
                newName: "IX_DailyEnergyBalances_AddressId");

            migrationBuilder.AddColumn<int>(
                name: "AddressId",
                table: "InverterReadings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AddressId",
                table: "ConsumptionReadings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_InverterReadings_AddressId",
                table: "InverterReadings",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumptionReadings_AddressId",
                table: "ConsumptionReadings",
                column: "AddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumptionReadings_Addresses",
                table: "ConsumptionReadings",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InverterReadings_Addresses",
                table: "InverterReadings",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InverterReadings_InverterInfos_InverterInfoId",
                table: "InverterReadings",
                column: "InverterInfoId",
                principalTable: "InverterInfos",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsumptionReadings_Addresses",
                table: "ConsumptionReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_InverterReadings_Addresses",
                table: "InverterReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_InverterReadings_InverterInfos_InverterInfoId",
                table: "InverterReadings");

            migrationBuilder.DropIndex(
                name: "IX_InverterReadings_AddressId",
                table: "InverterReadings");

            migrationBuilder.DropIndex(
                name: "IX_ConsumptionReadings_AddressId",
                table: "ConsumptionReadings");

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "InverterReadings");

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "ConsumptionReadings");

            migrationBuilder.RenameColumn(
                name: "AddressId",
                table: "ProviderSettlements",
                newName: "LocationId");

            migrationBuilder.RenameIndex(
                name: "IX_ProviderSettlements_AddressId",
                table: "ProviderSettlements",
                newName: "IX_ProviderSettlements_LocationId");

            migrationBuilder.RenameColumn(
                name: "InverterInfoId",
                table: "InverterReadings",
                newName: "LocationId");

            migrationBuilder.RenameIndex(
                name: "IX_InverterReadings_InverterInfoId",
                table: "InverterReadings",
                newName: "IX_InverterReadings_LocationId");

            migrationBuilder.RenameColumn(
                name: "AddressId",
                table: "DailyEnergyBalances",
                newName: "LocationId");

            migrationBuilder.RenameIndex(
                name: "IX_DailyEnergyBalances_AddressId",
                table: "DailyEnergyBalances",
                newName: "IX_DailyEnergyBalances_LocationId");

            migrationBuilder.AddColumn<int>(
                name: "InverterId",
                table: "InverterReadings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "ConsumptionReadings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InverterReadings_InverterId",
                table: "InverterReadings",
                column: "InverterId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumptionReadings_LocationId",
                table: "ConsumptionReadings",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumptionReadings_Addresses",
                table: "ConsumptionReadings",
                column: "LocationId",
                principalTable: "Addresses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InverterReadings_Addresses",
                table: "InverterReadings",
                column: "LocationId",
                principalTable: "Addresses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InverterReadings_InverterInfos",
                table: "InverterReadings",
                column: "InverterId",
                principalTable: "InverterInfos",
                principalColumn: "Id");
        }
    }
}
