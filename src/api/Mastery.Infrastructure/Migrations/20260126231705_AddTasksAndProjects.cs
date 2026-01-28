using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mastery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTasksAndProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    GoalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NextTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OutcomeNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RoleIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValueIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GoalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    EnergyCost = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Due = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Scheduling = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Completion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastRescheduleReason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    RescheduleCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ContextTags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DependencyTaskIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoleIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValueIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Milestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Milestones_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskMetricBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetricDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContributionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FixedValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskMetricBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskMetricBindings_MetricDefinitions_MetricDefinitionId",
                        column: x => x.MetricDefinitionId,
                        principalTable: "MetricDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskMetricBindings_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_ProjectId",
                table: "Milestones",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_ProjectId_DisplayOrder",
                table: "Milestones",
                columns: new[] { "ProjectId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_GoalId",
                table: "Projects",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_NextTaskId",
                table: "Projects",
                column: "NextTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_UserId",
                table: "Projects",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_UserId_Status",
                table: "Projects",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskMetricBindings_MetricDefinitionId",
                table: "TaskMetricBindings",
                column: "MetricDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskMetricBindings_TaskId",
                table: "TaskMetricBindings",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskMetricBindings_TaskId_MetricDefinitionId",
                table: "TaskMetricBindings",
                columns: new[] { "TaskId", "MetricDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_GoalId",
                table: "Tasks",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ProjectId",
                table: "Tasks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId",
                table: "Tasks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId_DisplayOrder",
                table: "Tasks",
                columns: new[] { "UserId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId_Status",
                table: "Tasks",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Milestones");

            migrationBuilder.DropTable(
                name: "TaskMetricBindings");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Tasks");
        }
    }
}
