using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QueueModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "called_at",
                table: "queue_tickets",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "called_by_staff_id",
                table: "queue_tickets",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "entered_room_at",
                table: "queue_tickets",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "left_at",
                table: "queue_tickets",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "room_id",
                table: "queue_tickets",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sequence",
                table: "queue_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "served_at",
                table: "queue_tickets",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "skip_count",
                table: "queue_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "skipped_at",
                table: "queue_tickets",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "exam_rooms",
                table: "branches",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.CreateIndex(
                name: "ix_queue_tickets_branch_id_service_date_sequence",
                table: "queue_tickets",
                columns: new[] { "branch_id", "service_date", "sequence" });

            migrationBuilder.CreateIndex(
                name: "ix_queue_tickets_branch_id_service_date_status",
                table: "queue_tickets",
                columns: new[] { "branch_id", "service_date", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_queue_tickets_customer_id_service_date",
                table: "queue_tickets",
                columns: new[] { "customer_id", "service_date" });

            // `sequence` splits the numeric part out of the printed number so the board can order
            // and the allocator can take MAX() without parsing strings. Existing rows arrive with
            // the column default of 0, which would sort them all first and make the next allocated
            // number 1 — colliding with an A001 that has already been called out in a waiting
            // room. Backfilled from the number that was actually printed.
            migrationBuilder.Sql(
                """
                UPDATE queue_tickets
                SET sequence = CAST(substring(number FROM '[0-9]+$') AS integer)
                WHERE sequence = 0
                  AND substring(number FROM '[0-9]+$') IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_queue_tickets_branch_id_service_date_sequence",
                table: "queue_tickets");

            migrationBuilder.DropIndex(
                name: "ix_queue_tickets_branch_id_service_date_status",
                table: "queue_tickets");

            migrationBuilder.DropIndex(
                name: "ix_queue_tickets_customer_id_service_date",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "called_at",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "called_by_staff_id",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "entered_room_at",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "left_at",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "room_id",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "sequence",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "served_at",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "skip_count",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "skipped_at",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "exam_rooms",
                table: "branches");
        }
    }
}
