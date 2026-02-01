using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mastery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTraceDebugFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InputCostUsd",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "OutputCostUsd",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "TotalCostUsd",
                table: "AgentRuns");

            migrationBuilder.AddColumn<int>(
                name: "FinalTier",
                table: "RecommendationTraces",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PolicyResultJson",
                table: "RecommendationTraces",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingWindowType",
                table: "RecommendationTraces",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "Tier0TriggeredRulesJson",
                table: "RecommendationTraces",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tier1EscalationReason",
                table: "RecommendationTraces",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tier1ScoresJson",
                table: "RecommendationTraces",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalDurationMs",
                table: "RecommendationTraces",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationTraces_CreatedAt",
                table: "RecommendationTraces",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationTraces_FinalTier",
                table: "RecommendationTraces",
                column: "FinalTier");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationTraces_ProcessingWindowType",
                table: "RecommendationTraces",
                column: "ProcessingWindowType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecommendationTraces_CreatedAt",
                table: "RecommendationTraces");

            migrationBuilder.DropIndex(
                name: "IX_RecommendationTraces_FinalTier",
                table: "RecommendationTraces");

            migrationBuilder.DropIndex(
                name: "IX_RecommendationTraces_ProcessingWindowType",
                table: "RecommendationTraces");

            migrationBuilder.DropColumn(
                name: "FinalTier",
                table: "RecommendationTraces");

            migrationBuilder.DropColumn(
                name: "PolicyResultJson",
                table: "RecommendationTraces");

            migrationBuilder.DropColumn(
                name: "ProcessingWindowType",
                table: "RecommendationTraces");

            migrationBuilder.DropColumn(
                name: "Tier0TriggeredRulesJson",
                table: "RecommendationTraces");

            migrationBuilder.DropColumn(
                name: "Tier1EscalationReason",
                table: "RecommendationTraces");

            migrationBuilder.DropColumn(
                name: "Tier1ScoresJson",
                table: "RecommendationTraces");

            migrationBuilder.DropColumn(
                name: "TotalDurationMs",
                table: "RecommendationTraces");

            migrationBuilder.AddColumn<decimal>(
                name: "InputCostUsd",
                table: "AgentRuns",
                type: "decimal(10,8)",
                precision: 10,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OutputCostUsd",
                table: "AgentRuns",
                type: "decimal(10,8)",
                precision: 10,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCostUsd",
                table: "AgentRuns",
                type: "decimal(10,8)",
                precision: 10,
                scale: 8,
                nullable: true);
        }
    }
}
