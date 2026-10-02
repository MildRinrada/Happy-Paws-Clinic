using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SchedulingAndAccountSelfService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "user_accounts",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "th");

            migrationBuilder.AddColumn<bool>(
                name: "notify_by_email",
                table: "user_accounts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "notify_by_line",
                table: "user_accounts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "notify_by_sms",
                table: "user_accounts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_two_factor_secret",
                table: "user_accounts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "recovery_code_hashes",
                table: "user_accounts",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.CreateTable(
                name: "assignment_rules",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    rules = table.Column<string>(type: "jsonb", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignment_rules", x => x.id);
                    table.CheckConstraint("ck_assignment_rules_singleton", "id = 'set_assignment'");
                });

            migrationBuilder.CreateTable(
                name: "staff_time_off",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    recorded_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_time_off", x => x.id);
                    table.ForeignKey(
                        name: "fk_staff_time_off_staff_membership_staff_id",
                        column: x => x.staff_id,
                        principalTable: "staff_memberships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_staff_time_off_staff_id_starts_at",
                table: "staff_time_off",
                columns: new[] { "staff_id", "starts_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignment_rules");

            migrationBuilder.DropTable(
                name: "staff_time_off");

            migrationBuilder.DropColumn(
                name: "language",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "notify_by_email",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "notify_by_line",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "notify_by_sms",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "pending_two_factor_secret",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "recovery_code_hashes",
                table: "user_accounts");
        }
    }
}
