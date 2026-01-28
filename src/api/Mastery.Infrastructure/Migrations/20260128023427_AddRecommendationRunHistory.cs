using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mastery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendationRunHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecommendationRunHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsersEvaluated = table.Column<int>(type: "int", nullable: false),
                    UsersProcessed = table.Column<int>(type: "int", nullable: false),
                    RecommendationsGenerated = table.Column<int>(type: "int", nullable: false),
                    Errors = table.Column<int>(type: "int", nullable: false),
                    ErrorDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationRunHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationRunHistory_StartedAt",
                table: "RecommendationRunHistory",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationRunHistory_Status",
                table: "RecommendationRunHistory",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecommendationRunHistory");
        }
    }
}
