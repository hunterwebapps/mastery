using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mastery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentRunLlmTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendationTraceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Stage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InputTokens = table.Column<int>(type: "int", nullable: false),
                    OutputTokens = table.Column<int>(type: "int", nullable: false),
                    CachedInputTokens = table.Column<int>(type: "int", nullable: true),
                    ReasoningTokens = table.Column<int>(type: "int", nullable: true),
                    LatencyMs = table.Column<int>(type: "int", nullable: false),
                    ErrorType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InputCostUsd = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: true),
                    OutputCostUsd = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: true),
                    TotalCostUsd = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: true),
                    SystemFingerprint = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterventionOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InterventionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContextKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WasAccepted = table.Column<bool>(type: "bit", nullable: false),
                    WasDismissed = table.Column<bool>(type: "bit", nullable: false),
                    WasCompleted = table.Column<bool>(type: "bit", nullable: true),
                    DismissReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResponseTimeSeconds = table.Column<int>(type: "int", nullable: true),
                    OriginalScore = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnergyLevel = table.Column<int>(type: "int", nullable: false),
                    CapacityUtilization = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    SeasonIntensity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterventionOutcomes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterventionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TitleTemplate = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RationaleTemplate = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ParametersSchema = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DefaultRecommendationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DefaultActionKind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DefaultTargetKind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterventionTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPlaybooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalOutcomes = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPlaybooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlaybookEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContextKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SuccessWeight = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    ObservationCount = table.Column<int>(type: "int", nullable: false),
                    AcceptanceRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    CompletionRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    AcceptanceCount = table.Column<int>(type: "int", nullable: false),
                    CompletionCount = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserPlaybookId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaybookEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaybookEntries_UserPlaybooks_UserPlaybookId",
                        column: x => x.UserPlaybookId,
                        principalTable: "UserPlaybooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_ErrorType",
                table: "AgentRuns",
                column: "ErrorType",
                filter: "[ErrorType] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_Model_StartedAt",
                table: "AgentRuns",
                columns: new[] { "Model", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_Provider_Model_StartedAt",
                table: "AgentRuns",
                columns: new[] { "Provider", "Model", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_RecommendationTraceId",
                table: "AgentRuns",
                column: "RecommendationTraceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_UserId_StartedAt",
                table: "AgentRuns",
                columns: new[] { "UserId", "StartedAt" },
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InterventionOutcomes_RecommendationId",
                table: "InterventionOutcomes",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_InterventionOutcomes_UserId",
                table: "InterventionOutcomes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InterventionOutcomes_UserId_ContextKey",
                table: "InterventionOutcomes",
                columns: new[] { "UserId", "ContextKey" });

            migrationBuilder.CreateIndex(
                name: "IX_InterventionOutcomes_UserId_RecommendationType",
                table: "InterventionOutcomes",
                columns: new[] { "UserId", "RecommendationType" });

            migrationBuilder.CreateIndex(
                name: "IX_InterventionTemplates_Category",
                table: "InterventionTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_InterventionTemplates_Code",
                table: "InterventionTemplates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterventionTemplates_IsActive",
                table: "InterventionTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PlaybookEntries_UserPlaybookId_RecommendationType_ContextKey",
                table: "PlaybookEntries",
                columns: new[] { "UserPlaybookId", "RecommendationType", "ContextKey" },
                unique: true,
                filter: "[UserPlaybookId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserPlaybooks_UserId",
                table: "UserPlaybooks",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentRuns");

            migrationBuilder.DropTable(
                name: "InterventionOutcomes");

            migrationBuilder.DropTable(
                name: "InterventionTemplates");

            migrationBuilder.DropTable(
                name: "PlaybookEntries");

            migrationBuilder.DropTable(
                name: "UserPlaybooks");
        }
    }
}
