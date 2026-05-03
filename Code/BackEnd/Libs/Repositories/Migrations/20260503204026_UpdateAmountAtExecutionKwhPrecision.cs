using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAmountAtExecutionKwhPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.TransferWorkflow', 'AmountAtExecutionKwh') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[TransferWorkflow] ALTER COLUMN [AmountAtExecutionKwh] decimal(18,5) NULL;
END
ELSE IF COL_LENGTH('dbo.TransferWorkflow', 'AmountKwnAtExecution') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[TransferWorkflow] ALTER COLUMN [AmountKwnAtExecution] decimal(18,5) NULL;
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
ELSE IF COL_LENGTH('dbo.TransferWorkflow', 'AmountKwnAtExecution') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[TransferWorkflow] ALTER COLUMN [AmountKwnAtExecution] decimal(18,2) NULL;
END
");
        }
    }
}
