using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "idempotency_records",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subject = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: true),
                    response_body = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotency_records", x => new { x.key, x.endpoint, x.subject });
                });

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_records_expires_at",
                table: "idempotency_records",
                column: "expires_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_records");
        }
    }
}
