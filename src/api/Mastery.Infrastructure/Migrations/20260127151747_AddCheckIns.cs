using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mastery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckIns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CheckInDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EnergyLevel = table.Column<int>(type: "int", nullable: true),
                    SelectedMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Top1Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Top1EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Top1FreeText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Intention = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EnergyLevelPm = table.Column<int>(type: "int", nullable: true),
                    StressLevel = table.Column<int>(type: "int", nullable: true),
                    Reflection = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BlockerCategory = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BlockerNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Top1Completed = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckIns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_UserId",
                table: "CheckIns",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_UserId_CheckInDate",
                table: "CheckIns",
                columns: new[] { "UserId", "CheckInDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_UserId_CheckInDate_Type",
                table: "CheckIns",
                columns: new[] { "UserId", "CheckInDate", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_UserId_Status",
                table: "CheckIns",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckIns");
        }
    }
}
