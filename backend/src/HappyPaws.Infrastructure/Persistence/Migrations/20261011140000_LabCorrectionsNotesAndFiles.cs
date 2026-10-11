using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LabCorrectionsNotesAndFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Lab corrections, review notes and files (docs/contract-extensions.md § 52, § 53, § 54).
            // Every new column on an existing table is nullable or has a default, so rows already
            // there read as they did: no correction, no note, no file. previous_values starts as an
            // empty list. lab_attachments is new and starts empty.
            migrationBuilder.AddColumn<string>(
                name: "amend_reason",
                table: "lab_results",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "amended_at",
                table: "lab_results",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "amended_by_staff_id",
                table: "lab_results",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "previous_values",
                table: "lab_results",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'");

            migrationBuilder.AddColumn<string>(
                name: "void_reason",
                table: "lab_results",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "voided_at",
                table: "lab_results",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "voided_by_staff_id",
                table: "lab_results",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "review_note",
                table: "lab_requests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "lab_attachments",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    lab_request_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    file_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    attached_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    attached_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lab_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_lab_attachments_lab_request_lab_request_id",
                        column: x => x.lab_request_id,
                        principalTable: "lab_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_lab_attachments_lab_request_id",
                table: "lab_attachments",
                column: "lab_request_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lab_attachments");

            migrationBuilder.DropColumn(
                name: "amend_reason",
                table: "lab_results");

            migrationBuilder.DropColumn(
                name: "amended_at",
                table: "lab_results");

            migrationBuilder.DropColumn(
                name: "amended_by_staff_id",
                table: "lab_results");

            migrationBuilder.DropColumn(
                name: "previous_values",
                table: "lab_results");

            migrationBuilder.DropColumn(
                name: "void_reason",
                table: "lab_results");

            migrationBuilder.DropColumn(
                name: "voided_at",
                table: "lab_results");

            migrationBuilder.DropColumn(
                name: "voided_by_staff_id",
                table: "lab_results");

            migrationBuilder.DropColumn(
                name: "review_note",
                table: "lab_requests");
        }
    }
}
