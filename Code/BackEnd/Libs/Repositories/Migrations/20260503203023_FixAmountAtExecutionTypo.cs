using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class FixAmountAtExecutionTypo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.TransferWorkflow', 'AmountKwnAtExecution') IS NOT NULL
   AND COL_LENGTH('dbo.TransferWorkflow', 'AmountAtExecutionKwh') IS NULL
BEGIN
    EXEC sp_rename 'dbo.TransferWorkflow.AmountKwnAtExecution', 'AmountAtExecutionKwh', 'COLUMN';
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.TransferWorkflow', 'AmountAtExecutionKwh') IS NOT NULL
   AND COL_LENGTH('dbo.TransferWorkflow', 'AmountKwnAtExecution') IS NULL
BEGIN
    EXEC sp_rename 'dbo.TransferWorkflow.AmountAtExecutionKwh', 'AmountKwnAtExecution', 'COLUMN';
END
");
        }
    }
}
