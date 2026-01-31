using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mastery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalHistoryBatchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxEntries");

            migrationBuilder.AddColumn<Guid>(
                name: "BatchId",
                table: "SignalProcessingHistory",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_SignalProcessingHistory_BatchId",
                table: "SignalProcessingHistory",
                column: "BatchId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SignalProcessingHistory_BatchId",
                table: "SignalProcessingHistory");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "SignalProcessingHistory");

            migrationBuilder.CreateTable(
                name: "OutboxEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DomainEventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LeaseHolder = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LeasedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Operation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEntries_EntityType_EntityId",
                table: "OutboxEntries",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEntries_Status_CreatedAt",
                table: "OutboxEntries",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEntries_Status_LeasedUntil",
                table: "OutboxEntries",
                columns: new[] { "Status", "LeasedUntil" },
                filter: "[Status] = 'Processing'");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEntries_Status_ProcessedAt",
                table: "OutboxEntries",
                columns: new[] { "Status", "ProcessedAt" },
                filter: "[Status] = 'Processed'");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEntries_UserId_Status_CreatedAt",
                table: "OutboxEntries",
                columns: new[] { "UserId", "Status", "CreatedAt" },
                filter: "[UserId] IS NOT NULL");
        }
    }
}
