using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddReadingsAndDailyBalanceLinks_NoCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsumptionReadings_Addresses",
                table: "ConsumptionReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_DailyEnergyBalances_Addresses",
                table: "DailyEnergyBalances");

            migrationBuilder.DropForeignKey(
                name: "FK_InverterReadings_Addresses",
                table: "InverterReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_InverterReadings_InverterInfos_InverterInfoId",
                table: "InverterReadings");

            migrationBuilder.AlterColumn<int>(
                name: "InverterInfoId",
                table: "InverterReadings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AddressId",
                table: "InverterReadings",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "AddressId",
                table: "DailyEnergyBalances",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InverterInfoId",
                table: "DailyEnergyBalances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "NetPerAddressKwh",
                table: "DailyEnergyBalances",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AddressId",
                table: "ConsumptionReadings",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "InverterInfoId",
                table: "ConsumptionReadings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DailyEnergyBalances_InverterInfoId",
                table: "DailyEnergyBalances",
                column: "InverterInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumptionReadings_InverterInfoId",
                table: "ConsumptionReadings",
                column: "InverterInfoId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumptionReadings_Addresses_AddressId",
                table: "ConsumptionReadings",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumptionReadings_InverterInfos",
                table: "ConsumptionReadings",
                column: "InverterInfoId",
                principalTable: "InverterInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyEnergyBalances_Addresses",
                table: "DailyEnergyBalances",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyEnergyBalances_InverterInfos_InverterInfoId",
                table: "DailyEnergyBalances",
                column: "InverterInfoId",
                principalTable: "InverterInfos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InverterReadings_Addresses_AddressId",
                table: "InverterReadings",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InverterReadings_InverterInfos",
                table: "InverterReadings",
                column: "InverterInfoId",
                principalTable: "InverterInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsumptionReadings_Addresses_AddressId",
                table: "ConsumptionReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_ConsumptionReadings_InverterInfos",
                table: "ConsumptionReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_DailyEnergyBalances_Addresses",
                table: "DailyEnergyBalances");

            migrationBuilder.DropForeignKey(
                name: "FK_DailyEnergyBalances_InverterInfos_InverterInfoId",
                table: "DailyEnergyBalances");

            migrationBuilder.DropForeignKey(
                name: "FK_InverterReadings_Addresses_AddressId",
                table: "InverterReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_InverterReadings_InverterInfos",
                table: "InverterReadings");

            migrationBuilder.DropIndex(
                name: "IX_DailyEnergyBalances_InverterInfoId",
                table: "DailyEnergyBalances");

            migrationBuilder.DropIndex(
                name: "IX_ConsumptionReadings_InverterInfoId",
                table: "ConsumptionReadings");

            migrationBuilder.DropColumn(
                name: "InverterInfoId",
                table: "DailyEnergyBalances");

            migrationBuilder.DropColumn(
                name: "NetPerAddressKwh",
                table: "DailyEnergyBalances");

            migrationBuilder.DropColumn(
                name: "InverterInfoId",
                table: "ConsumptionReadings");

            migrationBuilder.AlterColumn<int>(
                name: "InverterInfoId",
                table: "InverterReadings",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "AddressId",
                table: "InverterReadings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AddressId",
                table: "DailyEnergyBalances",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "AddressId",
                table: "ConsumptionReadings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumptionReadings_Addresses",
                table: "ConsumptionReadings",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyEnergyBalances_Addresses",
                table: "DailyEnergyBalances",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id");

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
    }
}
