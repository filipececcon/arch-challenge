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
            migrationBuilder.EnsureSchema(
                name: "outbox");

            migrationBuilder.CreateTable(
                name: "TB_ACCOUNT",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uuid", nullable: false),
                    ID_USER = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    VL_BALANCE = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DT_CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DT_UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ST_ACTIVE = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_ACCOUNT", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TB_OUTBOX",
                schema: "outbox",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uuid", nullable: false),
                    DS_KIND = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DS_PAYLOAD = table.Column<string>(type: "text", nullable: false),
                    DS_TARGET = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ST_PROCESSED = table.Column<bool>(type: "boolean", nullable: false),
                    DT_PROCESSED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NR_RETRY_COUNT = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DT_CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DT_UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ST_ACTIVE = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_OUTBOX", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TB_TRANSACTION",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uuid", nullable: false),
                    ID_ACCOUNT = table.Column<Guid>(type: "uuid", nullable: false),
                    ST_TYPE = table.Column<int>(type: "integer", nullable: false),
                    VL_AMOUNT = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DS_TRANSACTION = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    VL_BALANCE_AFTER = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DT_CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DT_UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ST_ACTIVE = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_TRANSACTION", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TB_TRANSACTION_TB_ACCOUNT_ID_ACCOUNT",
                        column: x => x.ID_ACCOUNT,
                        principalTable: "TB_ACCOUNT",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_ACCOUNT_USER",
                table: "TB_ACCOUNT",
                column: "ID_USER",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OUTBOX_PROCESSED_TARGET_CREATED",
                schema: "outbox",
                table: "TB_OUTBOX",
                columns: new[] { "ST_PROCESSED", "DS_TARGET", "DT_CREATED_AT" });

            migrationBuilder.CreateIndex(
                name: "IX_TB_TRANSACTION_ID_ACCOUNT",
                table: "TB_TRANSACTION",
                column: "ID_ACCOUNT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TB_OUTBOX",
                schema: "outbox");

            migrationBuilder.DropTable(
                name: "TB_TRANSACTION");

            migrationBuilder.DropTable(
                name: "TB_ACCOUNT");
        }
    }
}
