using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mastery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalsAndMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Why = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    Deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DependencyIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoleIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValueIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Goals_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MetricDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DefaultCadence = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DefaultAggregation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoalMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetricDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Target = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvaluationWindow = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aggregation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false, defaultValue: 1.0m),
                    SourceHint = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Baseline = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MinimumThreshold = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalMetrics_Goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "Goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoalMetrics_MetricDefinitions_MetricDefinitionId",
                        column: x => x.MetricDefinitionId,
                        principalTable: "MetricDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MetricObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetricDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ObservedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ObservedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrectedObservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsCorrected = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetricObservations_MetricDefinitions_MetricDefinitionId",
                        column: x => x.MetricDefinitionId,
                        principalTable: "MetricDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoalMetrics_GoalId",
                table: "GoalMetrics",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalMetrics_GoalId_MetricDefinitionId",
                table: "GoalMetrics",
                columns: new[] { "GoalId", "MetricDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoalMetrics_MetricDefinitionId",
                table: "GoalMetrics",
                column: "MetricDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_SeasonId",
                table: "Goals",
                column: "SeasonId",
                filter: "[SeasonId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_UserId",
                table: "Goals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_UserId_Status",
                table: "Goals",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MetricDefinitions_UserId",
                table: "MetricDefinitions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricDefinitions_UserId_Name",
                table: "MetricDefinitions",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetricObservations_CorrectedObservationId",
                table: "MetricObservations",
                column: "CorrectedObservationId",
                filter: "[CorrectedObservationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MetricObservations_MetricDefinitionId_ObservedOn",
                table: "MetricObservations",
                columns: new[] { "MetricDefinitionId", "ObservedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_MetricObservations_UserId_MetricDefinitionId_ObservedOn",
                table: "MetricObservations",
                columns: new[] { "UserId", "MetricDefinitionId", "ObservedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_MetricObservations_UserId_ObservedOn",
                table: "MetricObservations",
                columns: new[] { "UserId", "ObservedOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoalMetrics");

            migrationBuilder.DropTable(
                name: "MetricObservations");

            migrationBuilder.DropTable(
                name: "Goals");

            migrationBuilder.DropTable(
                name: "MetricDefinitions");
        }
    }
}
