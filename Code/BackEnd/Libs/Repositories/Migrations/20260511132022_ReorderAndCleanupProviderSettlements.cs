using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class ReorderAndCleanupProviderSettlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1 – remove stale AddressId FK, index, and column
            migrationBuilder.DropForeignKey(
                name: "FK_ProviderSettlements_Addresses_AddressId",
                table: "ProviderSettlements");

            migrationBuilder.DropForeignKey(
                name: "FK_ProviderSettlements_TransferWorkflow_TransferWorkflowId",
                table: "ProviderSettlements");

            migrationBuilder.DropIndex(
                name: "IX_ProviderSettlements_AddressId",
                table: "ProviderSettlements");

            migrationBuilder.DropIndex(
                name: "IX_ProviderSettlements_AddressId1",
                table: "ProviderSettlements");

            migrationBuilder.DropIndex(
                name: "IX_ProviderSettlements_TransferWorkflowId",
                table: "ProviderSettlements");

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "ProviderSettlements");

            // Step 2 – rebuild table with desired column order
            //   Id, TransferWorkflowId, SourceAddressId, DestinationAddressId,
            //   Day, SubmittedKwh, SettledKwh, RatePerKwh, MonetaryCredit,
            //   EnergyCreditKwh, SettlementMode, Note,
            //   CreatedAtUtc, CreatedBy, UpdatedAtUtc, UpdatedBy  (audit fields last)
            migrationBuilder.Sql("EXEC sp_rename N'ProviderSettlements', N'ProviderSettlements_Old';");
            // Rename the PK constraint so the new table can reuse the same name
            migrationBuilder.Sql("EXEC sp_rename N'PK_ProviderSettlements', N'PK_ProviderSettlements_Old';");

            migrationBuilder.Sql(@"
                CREATE TABLE [ProviderSettlements] (
                    [Id]                   int            NOT NULL IDENTITY,
                    [TransferWorkflowId]   int            NULL,
                    [SourceAddressId]      int            NOT NULL,
                    [DestinationAddressId] int            NOT NULL,
                    [Day]                  datetime2      NOT NULL,
                    [SubmittedKwh]         decimal(18, 5) NOT NULL,
                    [SettledKwh]           decimal(18, 5) NOT NULL,
                    [RatePerKwh]           decimal(18, 5) NOT NULL,
                    [MonetaryCredit]       decimal(18, 5) NOT NULL,
                    [EnergyCreditKwh]      decimal(18, 5) NOT NULL,
                    [SettlementMode]       int            NOT NULL,
                    [Note]                 nvarchar(max)  NULL,
                    [CreatedAtUtc]         datetime2      NOT NULL,
                    [CreatedBy]            nvarchar(max)  NOT NULL,
                    [UpdatedAtUtc]         datetime2      NULL,
                    [UpdatedBy]            nvarchar(max)  NULL,
                    CONSTRAINT [PK_ProviderSettlements] PRIMARY KEY ([Id])
                );");

            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT [ProviderSettlements] ON;
                INSERT INTO [ProviderSettlements]
                    ([Id],[TransferWorkflowId],[SourceAddressId],[DestinationAddressId],
                     [Day],[SubmittedKwh],[SettledKwh],[RatePerKwh],[MonetaryCredit],
                     [EnergyCreditKwh],[SettlementMode],[Note],
                     [CreatedAtUtc],[CreatedBy],[UpdatedAtUtc],[UpdatedBy])
                SELECT  [Id],[TransferWorkflowId],[SourceAddressId],[DestinationAddressId],
                        [Day],[SubmittedKwh],[SettledKwh],[RatePerKwh],[MonetaryCredit],
                        [EnergyCreditKwh],[SettlementMode],[Note],
                        [CreatedAtUtc],[CreatedBy],[UpdatedAtUtc],[UpdatedBy]
                FROM [ProviderSettlements_Old];
                SET IDENTITY_INSERT [ProviderSettlements] OFF;");

            migrationBuilder.Sql("DROP TABLE [ProviderSettlements_Old];");

            // Step 3 – recreate indexes and FK
            migrationBuilder.CreateIndex(
                name: "IX_ProviderSettlements_SourceAddressId",
                table: "ProviderSettlements",
                column: "SourceAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSettlements_TransferWorkflowId",
                table: "ProviderSettlements",
                column: "TransferWorkflowId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderSettlements_TransferWorkflow_TransferWorkflowId",
                table: "ProviderSettlements",
                column: "TransferWorkflowId",
                principalTable: "TransferWorkflow",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1 – drop indexes and FK from the reordered table
            migrationBuilder.DropForeignKey(
                name: "FK_ProviderSettlements_TransferWorkflow_TransferWorkflowId",
                table: "ProviderSettlements");

            migrationBuilder.DropIndex(
                name: "IX_ProviderSettlements_SourceAddressId",
                table: "ProviderSettlements");

            migrationBuilder.DropIndex(
                name: "IX_ProviderSettlements_TransferWorkflowId",
                table: "ProviderSettlements");

            // Step 2 – restore original column order (including nullable AddressId)
            migrationBuilder.Sql("EXEC sp_rename N'ProviderSettlements', N'ProviderSettlements_New';");
            // Rename the PK constraint so the restored table can reuse the same name
            migrationBuilder.Sql("EXEC sp_rename N'PK_ProviderSettlements', N'PK_ProviderSettlements_New';");

            migrationBuilder.Sql(@"
                CREATE TABLE [ProviderSettlements] (
                    [Id]                   int            NOT NULL IDENTITY,
                    [AddressId]            int            NULL,
                    [CreatedAtUtc]         datetime2      NOT NULL,
                    [CreatedBy]            nvarchar(max)  NOT NULL,
                    [Day]                  datetime2      NOT NULL,
                    [DestinationAddressId] int            NOT NULL,
                    [EnergyCreditKwh]      decimal(18, 5) NOT NULL,
                    [MonetaryCredit]       decimal(18, 5) NOT NULL,
                    [Note]                 nvarchar(max)  NULL,
                    [RatePerKwh]           decimal(18, 5) NOT NULL,
                    [SettledKwh]           decimal(18, 5) NOT NULL,
                    [SettlementMode]       int            NOT NULL,
                    [SourceAddressId]      int            NOT NULL,
                    [SubmittedKwh]         decimal(18, 5) NOT NULL,
                    [TransferWorkflowId]   int            NULL,
                    [UpdatedAtUtc]         datetime2      NULL,
                    [UpdatedBy]            nvarchar(max)  NULL,
                    CONSTRAINT [PK_ProviderSettlements] PRIMARY KEY ([Id])
                );");

            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT [ProviderSettlements] ON;
                INSERT INTO [ProviderSettlements]
                    ([Id],[AddressId],[CreatedAtUtc],[CreatedBy],[Day],
                     [DestinationAddressId],[EnergyCreditKwh],[MonetaryCredit],[Note],
                     [RatePerKwh],[SettledKwh],[SettlementMode],[SourceAddressId],
                     [SubmittedKwh],[TransferWorkflowId],[UpdatedAtUtc],[UpdatedBy])
                SELECT  [Id],NULL,[CreatedAtUtc],[CreatedBy],[Day],
                        [DestinationAddressId],[EnergyCreditKwh],[MonetaryCredit],[Note],
                        [RatePerKwh],[SettledKwh],[SettlementMode],[SourceAddressId],
                        [SubmittedKwh],[TransferWorkflowId],[UpdatedAtUtc],[UpdatedBy]
                FROM [ProviderSettlements_New];
                SET IDENTITY_INSERT [ProviderSettlements] OFF;");

            migrationBuilder.Sql("DROP TABLE [ProviderSettlements_New];");

            // Step 3 – restore original indexes and FKs
            migrationBuilder.CreateIndex(
                name: "IX_ProviderSettlements_AddressId",
                table: "ProviderSettlements",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSettlements_AddressId1",
                table: "ProviderSettlements",
                column: "SourceAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSettlements_TransferWorkflowId",
                table: "ProviderSettlements",
                column: "TransferWorkflowId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderSettlements_Addresses_AddressId",
                table: "ProviderSettlements",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderSettlements_TransferWorkflow_TransferWorkflowId",
                table: "ProviderSettlements",
                column: "TransferWorkflowId",
                principalTable: "TransferWorkflow",
                principalColumn: "Id");
        }
    }
}
