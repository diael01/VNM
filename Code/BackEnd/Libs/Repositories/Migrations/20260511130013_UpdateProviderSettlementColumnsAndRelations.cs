using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProviderSettlementColumnsAndRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProviderSettlements_Addresses",
                table: "ProviderSettlements");

            migrationBuilder.DropColumn(
                name: "ProcessedAtUtc",
                table: "ProviderSettlements");

            migrationBuilder.RenameColumn(
                name: "InjectedKwh",
                table: "ProviderSettlements",
                newName: "SubmittedKwh");

            migrationBuilder.RenameColumn(
                name: "AcceptedKwh",
                table: "ProviderSettlements",
                newName: "SettledKwh");

            migrationBuilder.AlterColumn<int>(
                name: "AddressId",
                table: "ProviderSettlements",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "DestinationAddressId",
                table: "ProviderSettlements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "ProviderSettlements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceAddressId",
                table: "ProviderSettlements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TransferWorkflowId",
                table: "ProviderSettlements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSettlements_AddressId1",
                table: "ProviderSettlements",
                column: "SourceAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSettlements_TransferWorkflowId",
                table: "ProviderSettlements",
                column: "TransferWorkflowId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderSettlements_Addresses_AddressId",
                table: "ProviderSettlements",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderSettlements_TransferWorkflow_TransferWorkflowId",
                table: "ProviderSettlements",
                column: "TransferWorkflowId",
                principalTable: "TransferWorkflow",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProviderSettlements_Addresses_AddressId",
                table: "ProviderSettlements");

            migrationBuilder.DropForeignKey(
                name: "FK_ProviderSettlements_TransferWorkflow_TransferWorkflowId",
                table: "ProviderSettlements");

            migrationBuilder.DropIndex(
                name: "IX_ProviderSettlements_AddressId1",
                table: "ProviderSettlements");

            migrationBuilder.DropIndex(
                name: "IX_ProviderSettlements_TransferWorkflowId",
                table: "ProviderSettlements");

            migrationBuilder.DropColumn(
                name: "DestinationAddressId",
                table: "ProviderSettlements");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "ProviderSettlements");

            migrationBuilder.DropColumn(
                name: "SourceAddressId",
                table: "ProviderSettlements");

            migrationBuilder.DropColumn(
                name: "TransferWorkflowId",
                table: "ProviderSettlements");

            migrationBuilder.RenameColumn(
                name: "SubmittedKwh",
                table: "ProviderSettlements",
                newName: "InjectedKwh");

            migrationBuilder.RenameColumn(
                name: "SettledKwh",
                table: "ProviderSettlements",
                newName: "AcceptedKwh");

            migrationBuilder.AlterColumn<int>(
                name: "AddressId",
                table: "ProviderSettlements",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAtUtc",
                table: "ProviderSettlements",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderSettlements_Addresses",
                table: "ProviderSettlements",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id");
        }
    }
}
