using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mastery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingWindowPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "MorningWindowStart",
                table: "UserProfiles",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(6, 0, 0)); // 6:00 AM

            migrationBuilder.AddColumn<TimeOnly>(
                name: "MorningWindowEnd",
                table: "UserProfiles",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(9, 0, 0)); // 9:00 AM

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EveningWindowStart",
                table: "UserProfiles",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(20, 0, 0)); // 8:00 PM

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EveningWindowEnd",
                table: "UserProfiles",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(22, 0, 0)); // 10:00 PM

            migrationBuilder.AddColumn<string>(
                name: "WeeklyReviewDay",
                table: "UserProfiles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Sunday");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "WeeklyReviewStart",
                table: "UserProfiles",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(17, 0, 0)); // 5:00 PM

            migrationBuilder.AddColumn<TimeOnly>(
                name: "WeeklyReviewEnd",
                table: "UserProfiles",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(20, 0, 0)); // 8:00 PM
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EveningWindowEnd",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "EveningWindowStart",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "MorningWindowEnd",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "MorningWindowStart",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "WeeklyReviewDay",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "WeeklyReviewEnd",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "WeeklyReviewStart",
                table: "UserProfiles");
        }
    }
}
