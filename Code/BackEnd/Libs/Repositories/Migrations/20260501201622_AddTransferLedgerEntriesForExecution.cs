using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferLedgerEntriesForExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransferLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransferWorkflowId = table.Column<int>(type: "int", nullable: false),
                    SourceAddressId = table.Column<int>(type: "int", nullable: false),
                    DestinationAddressId = table.Column<int>(type: "int", nullable: false),
                    BalanceDay = table.Column<DateOnly>(type: "date", nullable: false),
                    AmountKwh = table.Column<decimal>(type: "decimal(18,5)", nullable: false),
                    ExecutedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutionReference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferLedgerEntries_TransferWorkflow",
                        column: x => x.TransferWorkflowId,
                        principalTable: "TransferWorkflow",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransferLedgerEntries_ExecutedAtUtc",
                table: "TransferLedgerEntries",
                column: "ExecutedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TransferLedgerEntries_TransferWorkflowId",
                table: "TransferLedgerEntries",
                column: "TransferWorkflowId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransferLedgerEntries");
        }
    }
}
