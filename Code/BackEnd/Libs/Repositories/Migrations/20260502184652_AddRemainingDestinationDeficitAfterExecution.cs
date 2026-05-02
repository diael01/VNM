using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingDestinationDeficitAfterExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "RemainingSourceSurplusKwhAfterWorkflow",
                table: "TransferWorkflow",
                type: "decimal(18,5)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,5)");

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingDestinationDeficitKwhAfterWorkflow",
                table: "TransferWorkflow",
                type: "decimal(18,5)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemainingDestinationDeficitKwhAfterWorkflow",
                table: "TransferWorkflow");

            migrationBuilder.AlterColumn<decimal>(
                name: "RemainingSourceSurplusKwhAfterWorkflow",
                table: "TransferWorkflow",
                type: "decimal(18,5)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,5)",
                oldNullable: true);
        }
    }
}
