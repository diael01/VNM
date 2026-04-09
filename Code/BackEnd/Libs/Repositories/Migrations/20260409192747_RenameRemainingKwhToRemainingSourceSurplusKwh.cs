using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class RenameRemainingKwhToRemainingSourceSurplusKwh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RemainingKwh",
                table: "TransferWorkflow",
                newName: "RemainingSourceSurplusKwh");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RemainingSourceSurplusKwh",
                table: "TransferWorkflow",
                newName: "RemainingKwh");
        }
    }
}
