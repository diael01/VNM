using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class FixAmountAtExecutionColumnName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename AmountKwhAtExecution -> AmountAtExecutionKwh if it exists and the target doesn't
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.TransferWorkflow', 'AmountKwhAtExecution') IS NOT NULL
   AND COL_LENGTH('dbo.TransferWorkflow', 'AmountAtExecutionKwh') IS NULL
BEGIN
    EXEC sp_rename 'dbo.TransferWorkflow.AmountKwhAtExecution', 'AmountAtExecutionKwh', 'COLUMN';
END
");
            // Fix precision to decimal(18,5)
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.TransferWorkflow', 'AmountAtExecutionKwh') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[TransferWorkflow] ALTER COLUMN [AmountAtExecutionKwh] decimal(18,5) NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.TransferWorkflow', 'AmountAtExecutionKwh') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[TransferWorkflow] ALTER COLUMN [AmountAtExecutionKwh] decimal(18,2) NULL;
END
");
        }
    }
}
