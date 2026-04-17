using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferPoliciesAndAuditInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "TransferWorkflow",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "TransferWorkflow",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TransferWorkflow",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "TransferRules",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "TransferRules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "TransferRules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TransferRules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "TransferRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "TransferRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TransferRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ProviderSettlements",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProviderSettlements",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "ProviderSettlements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ProviderSettlements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "InverterReadings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "InverterReadings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "InverterReadings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "InverterReadings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "InverterInfos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "InverterInfos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "InverterInfos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "InverterInfos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "DailyEnergyBalances",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "DailyEnergyBalances",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "DailyEnergyBalances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "DailyEnergyBalances",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ConsumptionReadings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ConsumptionReadings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "ConsumptionReadings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ConsumptionReadings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "AspNetUserClaims",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AspNetUserClaims",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "AspNetUserClaims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "AspNetUserClaims",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "AspNetRoles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AspNetRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "AspNetRoles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "AspNetRoles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "AspNetRoleClaims",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AspNetRoleClaims",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "AspNetRoleClaims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "AspNetRoleClaims",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Addresses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Addresses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Addresses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Addresses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SourceTransferPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceAddressId = table.Column<int>(type: "int", nullable: false),
                    DistributionMode = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceTransferPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceTransferPolicies_Addresses_SourceAddressId",
                        column: x => x.SourceAddressId,
                        principalTable: "Addresses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DestinationTransferRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceTransferPolicyId = table.Column<int>(type: "int", nullable: false),
                    DestinationAddressId = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    WeightPercent = table.Column<decimal>(type: "decimal(18,5)", nullable: true),
                    MaxDailyKwh = table.Column<decimal>(type: "decimal(18,5)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestinationTransferRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestinationTransferRules_Addresses_DestinationAddressId",
                        column: x => x.DestinationAddressId,
                        principalTable: "Addresses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DestinationTransferRules_SourceTransferPolicies_SourceTransferPolicyId",
                        column: x => x.SourceTransferPolicyId,
                        principalTable: "SourceTransferPolicies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DestinationTransferRules_DestinationAddressId",
                table: "DestinationTransferRules",
                column: "DestinationAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_DestinationTransferRules_SourceTransferPolicyId",
                table: "DestinationTransferRules",
                column: "SourceTransferPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceTransferPolicies_SourceAddressId",
                table: "SourceTransferPolicies",
                column: "SourceAddressId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DestinationTransferRules");

            migrationBuilder.DropTable(
                name: "SourceTransferPolicies");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "TransferWorkflow");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "TransferWorkflow");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TransferWorkflow");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "TransferRules");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "TransferRules");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "TransferRules");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TransferRules");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "TransferRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "TransferRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TransferRequests");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ProviderSettlements");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProviderSettlements");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "ProviderSettlements");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ProviderSettlements");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "InverterReadings");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "InverterReadings");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "InverterReadings");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "InverterReadings");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "InverterInfos");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "InverterInfos");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "InverterInfos");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "InverterInfos");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "DailyEnergyBalances");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DailyEnergyBalances");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "DailyEnergyBalances");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "DailyEnergyBalances");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ConsumptionReadings");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ConsumptionReadings");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "ConsumptionReadings");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ConsumptionReadings");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "AspNetUserClaims");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AspNetUserClaims");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "AspNetUserClaims");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AspNetUserClaims");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "AspNetRoleClaims");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AspNetRoleClaims");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "AspNetRoleClaims");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AspNetRoleClaims");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Addresses");
        }
    }
}
