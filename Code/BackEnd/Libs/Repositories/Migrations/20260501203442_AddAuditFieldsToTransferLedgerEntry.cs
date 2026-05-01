using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToTransferLedgerEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "TransferLedgerEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "TransferLedgerEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "TransferLedgerEntries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TransferLedgerEntries",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "TransferLedgerEntries");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "TransferLedgerEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "TransferLedgerEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TransferLedgerEntries");
        }
    }
}
