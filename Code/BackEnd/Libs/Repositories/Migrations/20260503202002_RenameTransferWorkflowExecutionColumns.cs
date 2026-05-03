using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class RenameTransferWorkflowExecutionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RemainingSourceSurplusKwhAfterWorkflow",
                table: "TransferWorkflow",
                newName: "SourceSurplusKwhAtExecution");

            migrationBuilder.RenameColumn(
                name: "RemainingDestinationDeficitKwhAfterWorkflow",
                table: "TransferWorkflow",
                newName: "DestinationDeficitKwhAtExecution");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SourceSurplusKwhAtExecution",
                table: "TransferWorkflow",
                newName: "RemainingSourceSurplusKwhAfterWorkflow");

            migrationBuilder.RenameColumn(
                name: "DestinationDeficitKwhAtExecution",
                table: "TransferWorkflow",
                newName: "RemainingDestinationDeficitKwhAfterWorkflow");
        }
    }
}
