using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mastery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiagnosticSignals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    EvidenceMetric = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EvidenceCurrentValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EvidenceThresholdValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    EvidenceDetail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DetectedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedByRecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticSignals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Context = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TargetKind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetEntityTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ActionKind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Rationale = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ActionPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Score = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DismissReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SignalIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationTraces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StateSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SignalsSummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CandidateListJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PromptVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModelVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RawLlmResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SelectionMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationTraces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationTraces_Recommendations_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "Recommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_UserId",
                table: "Recommendations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_UserId_Context_Status",
                table: "Recommendations",
                columns: new[] { "UserId", "Context", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_UserId_Status",
                table: "Recommendations",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationTraces_RecommendationId",
                table: "RecommendationTraces",
                column: "RecommendationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiagnosticSignals");

            migrationBuilder.DropTable(
                name: "RecommendationTraces");

            migrationBuilder.DropTable(
                name: "Recommendations");
        }
    }
}
