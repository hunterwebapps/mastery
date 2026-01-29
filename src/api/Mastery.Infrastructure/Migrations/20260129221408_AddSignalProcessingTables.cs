using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mastery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalProcessingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiagnosticSignals");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "MetricObservations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "MetricObservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "MetricObservations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SignalEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventDataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WindowType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScheduledWindowStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TargetEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeasedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeaseHolder = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProcessingTier = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SkipReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignalProcessingHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    WindowType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignalsReceived = table.Column<int>(type: "int", nullable: false),
                    SignalsProcessed = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SignalsSkipped = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SignalIdsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FinalTier = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Tier0RulesTriggered = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Tier1CombinedScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    Tier2Executed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RecommendationsGenerated = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RecommendationIdsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StateDeltaSummaryJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalProcessingHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignalEntries_Deduplication",
                table: "SignalEntries",
                columns: new[] { "UserId", "EventType", "TargetEntityType", "TargetEntityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalEntries_Priority_Status_CreatedAt",
                table: "SignalEntries",
                columns: new[] { "Priority", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalEntries_Status_ExpiresAt",
                table: "SignalEntries",
                columns: new[] { "Status", "ExpiresAt" },
                filter: "[Status] = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_SignalEntries_Status_LeasedUntil",
                table: "SignalEntries",
                columns: new[] { "Status", "LeasedUntil" },
                filter: "[Status] = 'Processing'");

            migrationBuilder.CreateIndex(
                name: "IX_SignalEntries_UserId_Status_CreatedAt",
                table: "SignalEntries",
                columns: new[] { "UserId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalEntries_WindowType_Status_ScheduledWindowStart",
                table: "SignalEntries",
                columns: new[] { "WindowType", "Status", "ScheduledWindowStart" },
                filter: "[Status] = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_SignalProcessingHistory_StartedAt",
                table: "SignalProcessingHistory",
                column: "StartedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_SignalProcessingHistory_UserId_StartedAt",
                table: "SignalProcessingHistory",
                columns: new[] { "UserId", "StartedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_SignalProcessingHistory_WindowType_StartedAt",
                table: "SignalProcessingHistory",
                columns: new[] { "WindowType", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignalEntries");

            migrationBuilder.DropTable(
                name: "SignalProcessingHistory");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "MetricObservations");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "MetricObservations");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "MetricObservations");

            migrationBuilder.CreateTable(
                name: "DiagnosticSignals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DetectedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ResolvedByRecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EvidenceCurrentValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EvidenceDetail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EvidenceMetric = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EvidenceThresholdValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticSignals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticSignals_UserId",
                table: "DiagnosticSignals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticSignals_UserId_IsActive",
                table: "DiagnosticSignals",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticSignals_UserId_Type",
                table: "DiagnosticSignals",
                columns: new[] { "UserId", "Type" });
        }
    }
}
