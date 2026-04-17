using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceTransferSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SourceTransferSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceTransferPolicyId = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ScheduleType = table.Column<int>(type: "int", nullable: false),
                    ExecutionMode = table.Column<int>(type: "int", nullable: false),
                    StartDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeOfDayUtc = table.Column<TimeSpan>(type: "time", nullable: true),
                    IntervalMinutes = table.Column<int>(type: "int", nullable: true),
                    DayOfWeek = table.Column<int>(type: "int", nullable: true),
                    DayOfMonth = table.Column<int>(type: "int", nullable: true),
                    LastRunUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRunUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceTransferSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceTransferSchedules_SourceTransferPolicies_SourceTransferPolicyId",
                        column: x => x.SourceTransferPolicyId,
                        principalTable: "SourceTransferPolicies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SourceTransferSchedules_SourceTransferPolicyId",
                table: "SourceTransferSchedules",
                column: "SourceTransferPolicyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SourceTransferSchedules");
        }
    }
}
