using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mastery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHabits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Habits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Why = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Schedule = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Policy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Full"),
                    CurrentStreak = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AdherenceRate7Day = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    GoalIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoleIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValueIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Habits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HabitMetricBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HabitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_HabitMetricBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HabitMetricBindings_Habits_HabitId",
                        column: x => x.HabitId,
                        principalTable: "Habits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HabitMetricBindings_MetricDefinitions_MetricDefinitionId",
                        column: x => x.MetricDefinitionId,
                        principalTable: "MetricDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HabitOccurrences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HabitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ModeUsed = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EnteredValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MissReason = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RescheduledTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabitOccurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HabitOccurrences_Habits_HabitId",
                        column: x => x.HabitId,
                        principalTable: "Habits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HabitVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HabitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DefaultValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    EnergyCost = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    CountsAsCompletion = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabitVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HabitVariants_Habits_HabitId",
                        column: x => x.HabitId,
                        principalTable: "Habits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HabitMetricBindings_HabitId",
                table: "HabitMetricBindings",
                column: "HabitId");

            migrationBuilder.CreateIndex(
                name: "IX_HabitMetricBindings_HabitId_MetricDefinitionId",
                table: "HabitMetricBindings",
                columns: new[] { "HabitId", "MetricDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HabitMetricBindings_MetricDefinitionId",
                table: "HabitMetricBindings",
                column: "MetricDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_HabitOccurrences_HabitId_ScheduledOn",
                table: "HabitOccurrences",
                columns: new[] { "HabitId", "ScheduledOn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HabitOccurrences_HabitId_Status",
                table: "HabitOccurrences",
                columns: new[] { "HabitId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_HabitOccurrences_ScheduledOn",
                table: "HabitOccurrences",
                column: "ScheduledOn");

            migrationBuilder.CreateIndex(
                name: "IX_Habits_UserId",
                table: "Habits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Habits_UserId_DisplayOrder",
                table: "Habits",
                columns: new[] { "UserId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Habits_UserId_Status",
                table: "Habits",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_HabitVariants_HabitId",
                table: "HabitVariants",
                column: "HabitId");

            migrationBuilder.CreateIndex(
                name: "IX_HabitVariants_HabitId_Mode",
                table: "HabitVariants",
                columns: new[] { "HabitId", "Mode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HabitMetricBindings");

            migrationBuilder.DropTable(
                name: "HabitOccurrences");

            migrationBuilder.DropTable(
                name: "HabitVariants");

            migrationBuilder.DropTable(
                name: "Habits");
        }
    }
}
