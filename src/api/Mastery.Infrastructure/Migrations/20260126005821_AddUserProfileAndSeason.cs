using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mastery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileAndSeason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ActualEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SuccessStatement = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Intensity = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    Outcome = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FocusGoalIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FocusRoleIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NonNegotiables = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timezone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OnboardingVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CurrentSeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CoachingStyle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExplanationVerbosity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NudgeLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NotificationChannels = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckInMorningTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CheckInEveningTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    DefaultTaskDurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    AutoScheduleHabits = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    BufferBetweenTasksMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    ShareProgressWithCoach = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AllowAnonymousAnalytics = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MaxPlannedMinutesWeekday = table.Column<int>(type: "int", nullable: false, defaultValue: 480),
                    MaxPlannedMinutesWeekend = table.Column<int>(type: "int", nullable: false, defaultValue: 240),
                    BlockedTimeWindows = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NoNotificationsWindows = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HealthNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ContentBoundaries = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Roles = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Values = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Seasons_CurrentSeasonId",
                        column: x => x.CurrentSeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_UserId",
                table: "Seasons",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_UserId_StartDate",
                table: "Seasons",
                columns: new[] { "UserId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_CurrentSeasonId",
                table: "UserProfiles",
                column: "CurrentSeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "Seasons");
        }
    }
}
