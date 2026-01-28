using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mastery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExperiments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Experiments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedFrom = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Hypothesis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MeasurementPlan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDatePlanned = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDateActual = table.Column<DateOnly>(type: "date", nullable: true),
                    LinkedGoalIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experiments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExperimentNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExperimentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExperimentNotes_Experiments_ExperimentId",
                        column: x => x.ExperimentId,
                        principalTable: "Experiments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExperimentResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExperimentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BaselineValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    RunValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Delta = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DeltaPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    OutcomeClassification = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ComplianceRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    NarrativeSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExperimentResults_Experiments_ExperimentId",
                        column: x => x.ExperimentId,
                        principalTable: "Experiments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentNotes_ExperimentId",
                table: "ExperimentNotes",
                column: "ExperimentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentResults_ExperimentId",
                table: "ExperimentResults",
                column: "ExperimentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Experiments_UserId_ActiveUnique",
                table: "Experiments",
                column: "UserId",
                unique: true,
                filter: "[Status] = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_Experiments_UserId_Status",
                table: "Experiments",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExperimentNotes");

            migrationBuilder.DropTable(
                name: "ExperimentResults");

            migrationBuilder.DropTable(
                name: "Experiments");
        }
    }
}
