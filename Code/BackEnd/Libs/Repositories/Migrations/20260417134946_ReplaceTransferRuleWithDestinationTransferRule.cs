using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceTransferRuleWithDestinationTransferRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransferWorkflow_TransferRules_TransferRuleId",
                table: "TransferWorkflow");

            migrationBuilder.DropTable(
                name: "TransferRules");

            migrationBuilder.RenameColumn(
                name: "TransferRuleId",
                table: "TransferWorkflow",
                newName: "DestinationTransferRuleId");

            migrationBuilder.RenameIndex(
                name: "IX_TransferWorkflow_TransferRuleId",
                table: "TransferWorkflow",
                newName: "IX_TransferWorkflow_DestinationTransferRuleId");

            migrationBuilder.AddColumn<int>(
                name: "DistributionMode",
                table: "DestinationTransferRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE [TransferWorkflow] SET [DestinationTransferRuleId] = NULL;");

            migrationBuilder.AddForeignKey(
                name: "FK_TransferWorkflow_DestinationTransferRules_DestinationTransferRuleId",
                table: "TransferWorkflow",
                column: "DestinationTransferRuleId",
                principalTable: "DestinationTransferRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransferWorkflow_DestinationTransferRules_DestinationTransferRuleId",
                table: "TransferWorkflow");

            migrationBuilder.DropColumn(
                name: "DistributionMode",
                table: "DestinationTransferRules");

            migrationBuilder.RenameColumn(
                name: "DestinationTransferRuleId",
                table: "TransferWorkflow",
                newName: "TransferRuleId");

            migrationBuilder.RenameIndex(
                name: "IX_TransferWorkflow_DestinationTransferRuleId",
                table: "TransferWorkflow",
                newName: "IX_TransferWorkflow_TransferRuleId");

            migrationBuilder.CreateTable(
                name: "TransferRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DestinationAddressId = table.Column<int>(type: "int", nullable: false),
                    SourceAddressId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DistributionMode = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MaxDailyKwh = table.Column<decimal>(type: "decimal(18,5)", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WeightPercent = table.Column<decimal>(type: "decimal(18,5)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferRules_Addresses_DestinationAddressId",
                        column: x => x.DestinationAddressId,
                        principalTable: "Addresses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransferRules_Addresses_SourceAddressId",
                        column: x => x.SourceAddressId,
                        principalTable: "Addresses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransferRules_DestinationAddressId",
                table: "TransferRules",
                column: "DestinationAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRules_SourceAddressId",
                table: "TransferRules",
                column: "SourceAddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransferWorkflow_TransferRules_TransferRuleId",
                table: "TransferWorkflow",
                column: "TransferRuleId",
                principalTable: "TransferRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
