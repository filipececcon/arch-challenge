using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema("control");
            migrationBuilder.EnsureSchema("outbox");

            migrationBuilder.CreateTable(
                name: "TB_TRANSACTION",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uuid", nullable: false),
                    ST_TYPE = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    VL_AMOUNT = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DS_TRANSACTION = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DT_CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DT_UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ST_ACTIVE = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_TRANSACTION", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TB_OUTBOX_EVENT",
                schema: "outbox",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uuid", nullable: false),
                    DS_EVENT_TYPE = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DS_PAYLOAD = table.Column<string>(type: "text", nullable: false),
                    ST_PROCESSED = table.Column<bool>(type: "boolean", nullable: false),
                    DT_PROCESSED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NR_RETRY_COUNT = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DT_CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DT_UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ST_ACTIVE = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_OUTBOX_EVENT", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OUTBOX_EVENT_PROCESSED_CREATED",
                schema: "outbox",
                table: "TB_OUTBOX_EVENT",
                columns: new[] { "ST_PROCESSED", "DT_CREATED_AT" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OUTBOX_EVENT_PROCESSED_CREATED",
                schema: "outbox",
                table: "TB_OUTBOX_EVENT");

            migrationBuilder.DropTable(name: "TB_OUTBOX_EVENT", schema: "outbox");

            migrationBuilder.DropTable(name: "TB_TRANSACTION");
        }
    }
}
