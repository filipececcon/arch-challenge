using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchChallenge.Dashboard.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dashboard");

            migrationBuilder.CreateTable(
                name: "daily_consolidations",
                schema: "dashboard",
                columns: table => new
                {
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalCredits = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDebits = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_consolidations", x => x.date);
                });

            migrationBuilder.CreateTable(
                name: "processed_integration_events",
                schema: "dashboard",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_integration_events", x => x.event_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_consolidations",
                schema: "dashboard");

            migrationBuilder.DropTable(
                name: "processed_integration_events",
                schema: "dashboard");
        }
    }
}
