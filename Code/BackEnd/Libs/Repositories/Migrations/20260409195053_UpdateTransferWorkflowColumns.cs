using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTransferWorkflowColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequestedKwh",
                table: "TransferWorkflow",
                newName: "SourceSurplusKwhAtWorkflow");

            migrationBuilder.RenameColumn(
                name: "RemainingSourceSurplusKwh",
                table: "TransferWorkflow",
                newName: "RemainingSourceSurplusKwhAfterWorkflow");

            migrationBuilder.RenameColumn(
                name: "AllocatedKwh",
                table: "TransferWorkflow",
                newName: "DestinationDeficitKwhAtWorkflow");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountKwh",
                table: "TransferWorkflow",
                type: "decimal(18,5)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountKwh",
                table: "TransferWorkflow");

            migrationBuilder.RenameColumn(
                name: "SourceSurplusKwhAtWorkflow",
                table: "TransferWorkflow",
                newName: "RequestedKwh");

            migrationBuilder.RenameColumn(
                name: "RemainingSourceSurplusKwhAfterWorkflow",
                table: "TransferWorkflow",
                newName: "RemainingSourceSurplusKwh");

            migrationBuilder.RenameColumn(
                name: "DestinationDeficitKwhAtWorkflow",
                table: "TransferWorkflow",
                newName: "AllocatedKwh");
        }
    }
}
