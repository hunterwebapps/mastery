using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mastery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnifyOutboxAndSignalProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SignalEntries_Status_Priority_NextProcessAfter",
                table: "SignalEntries");

            migrationBuilder.DropColumn(
                name: "DeferralCount",
                table: "SignalEntries");

            migrationBuilder.DropColumn(
                name: "NextProcessAfter",
                table: "SignalEntries");

            migrationBuilder.AddColumn<string>(
                name: "DomainEventType",
                table: "OutboxEntries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DomainEventType",
                table: "OutboxEntries");

            migrationBuilder.AddColumn<int>(
                name: "DeferralCount",
                table: "SignalEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextProcessAfter",
                table: "SignalEntries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignalEntries_Status_Priority_NextProcessAfter",
                table: "SignalEntries",
                columns: new[] { "Status", "Priority", "NextProcessAfter", "CreatedAt" },
                filter: "[Status] = 'Pending'");
        }
    }
}
