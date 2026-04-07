using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class RenameTransferExecutionTableToTransferWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransferExecutions_Addresses_DestinationAddressId",
                table: "TransferExecutions");

            migrationBuilder.DropForeignKey(
                name: "FK_TransferExecutions_Addresses_SourceAddressId",
                table: "TransferExecutions");

            migrationBuilder.DropForeignKey(
                name: "FK_TransferExecutions_TransferRules_TransferRuleId",
                table: "TransferExecutions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransferExecutions",
                table: "TransferExecutions");

            migrationBuilder.RenameTable(
                name: "TransferExecutions",
                newName: "TransferWorkflow");

            migrationBuilder.RenameIndex(
                name: "IX_TransferExecutions_TransferRuleId",
                table: "TransferWorkflow",
                newName: "IX_TransferWorkflow_TransferRuleId");

            migrationBuilder.RenameIndex(
                name: "IX_TransferExecutions_SourceAddressId",
                table: "TransferWorkflow",
                newName: "IX_TransferWorkflow_SourceAddressId");

            migrationBuilder.RenameIndex(
                name: "IX_TransferExecutions_DestinationAddressId",
                table: "TransferWorkflow",
                newName: "IX_TransferWorkflow_DestinationAddressId");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "TransferWorkflow",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightPercent",
                table: "TransferWorkflow",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransferWorkflow",
                table: "TransferWorkflow",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TransferWorkflow_Addresses_DestinationAddressId",
                table: "TransferWorkflow",
                column: "DestinationAddressId",
                principalTable: "Addresses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TransferWorkflow_Addresses_SourceAddressId",
                table: "TransferWorkflow",
                column: "SourceAddressId",
                principalTable: "Addresses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TransferWorkflow_TransferRules_TransferRuleId",
                table: "TransferWorkflow",
                column: "TransferRuleId",
                principalTable: "TransferRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransferWorkflow_Addresses_DestinationAddressId",
                table: "TransferWorkflow");

            migrationBuilder.DropForeignKey(
                name: "FK_TransferWorkflow_Addresses_SourceAddressId",
                table: "TransferWorkflow");

            migrationBuilder.DropForeignKey(
                name: "FK_TransferWorkflow_TransferRules_TransferRuleId",
                table: "TransferWorkflow");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransferWorkflow",
                table: "TransferWorkflow");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "TransferWorkflow");

            migrationBuilder.DropColumn(
                name: "WeightPercent",
                table: "TransferWorkflow");

            migrationBuilder.RenameTable(
                name: "TransferWorkflow",
                newName: "TransferExecutions");

            migrationBuilder.RenameIndex(
                name: "IX_TransferWorkflow_TransferRuleId",
                table: "TransferExecutions",
                newName: "IX_TransferExecutions_TransferRuleId");

            migrationBuilder.RenameIndex(
                name: "IX_TransferWorkflow_SourceAddressId",
                table: "TransferExecutions",
                newName: "IX_TransferExecutions_SourceAddressId");

            migrationBuilder.RenameIndex(
                name: "IX_TransferWorkflow_DestinationAddressId",
                table: "TransferExecutions",
                newName: "IX_TransferExecutions_DestinationAddressId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransferExecutions",
                table: "TransferExecutions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TransferExecutions_Addresses_DestinationAddressId",
                table: "TransferExecutions",
                column: "DestinationAddressId",
                principalTable: "Addresses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TransferExecutions_Addresses_SourceAddressId",
                table: "TransferExecutions",
                column: "SourceAddressId",
                principalTable: "Addresses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TransferExecutions_TransferRules_TransferRuleId",
                table: "TransferExecutions",
                column: "TransferRuleId",
                principalTable: "TransferRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
