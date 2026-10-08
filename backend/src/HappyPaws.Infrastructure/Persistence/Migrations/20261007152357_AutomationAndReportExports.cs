using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AutomationAndReportExports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "automation_rules",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    trigger = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    offset_days = table.Column<int>(type: "integer", nullable: true),
                    offset_hours = table.Column<int>(type: "integer", nullable: true),
                    send_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    species = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    vaccine_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    service_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    unconfirmed_only = table.Column<bool>(type: "boolean", nullable: false),
                    channel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    template_key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_automation_rules", x => x.id);
                    table.CheckConstraint("ck_automation_rules_one_offset", "(offset_days IS NULL) <> (offset_hours IS NULL)");
                });

            migrationBuilder.CreateTable(
                name: "report_exports",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    report = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    all_branches = table.Column<bool>(type: "boolean", nullable: false),
                    branch_ids = table.Column<string[]>(type: "text[]", nullable: false),
                    requested_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    file_key = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    file_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    row_count = table.Column<int>(type: "integer", nullable: true),
                    error_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_exports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "automation_dispatches",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    rule_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    subject_kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    subject_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    due_key = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    scheduled_for = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    claimed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    claimed_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    error_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_automation_dispatches", x => x.id);
                    table.ForeignKey(
                        name: "fk_automation_dispatches_automation_rule_rule_id",
                        column: x => x.rule_id,
                        principalTable: "automation_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_automation_dispatches_open_claims",
                table: "automation_dispatches",
                column: "claimed_at",
                filter: "status = 'Claimed'");

            migrationBuilder.CreateIndex(
                name: "ix_automation_dispatches_rule_id_claimed_at",
                table: "automation_dispatches",
                columns: new[] { "rule_id", "claimed_at" });

            migrationBuilder.CreateIndex(
                name: "ux_automation_dispatches_reminder",
                table: "automation_dispatches",
                columns: new[] { "rule_id", "subject_kind", "subject_id", "due_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_automation_rules_branch_id",
                table: "automation_rules",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_automation_rules_enabled",
                table: "automation_rules",
                column: "enabled",
                filter: "enabled");

            migrationBuilder.CreateIndex(
                name: "ix_report_exports_queue",
                table: "report_exports",
                column: "created_at",
                filter: "status = 'Queued'");

            migrationBuilder.CreateIndex(
                name: "ix_report_exports_running",
                table: "report_exports",
                column: "started_at",
                filter: "status = 'Running'");

            // The clinic's starter reminders, the ones the contract names: a vaccine 14 days ahead,
            // and visits at T-24h and T-2h ("reminder ตั้งไว้ T-24h และ T-2h"), plus the plan
            // sessions. Seeded switched OFF. The contract does not say whether a new clinic should
            // start messaging its customers on its own, and a rule that does so unasked is the
            // worse default; a clinic previews one with /simulate and opts in.
            migrationBuilder.Sql(
                """
                INSERT INTO automation_rules
                    (id, name, trigger, offset_days, offset_hours, send_time, unconfirmed_only,
                     channel, template_key, enabled, created_at)
                VALUES
                    ('atm_starter_vaccine_14d', 'เตือนวัคซีนล่วงหน้า 14 วัน', 'VaccineDue', 14, NULL, '09:00', false,
                     'Auto', 'vaccine.due', false, now()),
                    ('atm_starter_visit_24h', 'เตือนนัดล่วงหน้า 24 ชั่วโมง', 'AppointmentUpcoming', NULL, 24, NULL, false,
                     'Auto', 'appointment.reminder', false, now()),
                    ('atm_starter_visit_2h', 'เตือนนัดล่วงหน้า 2 ชั่วโมง', 'AppointmentUpcoming', NULL, 2, NULL, false,
                     'Auto', 'appointment.reminder', false, now()),
                    ('atm_starter_plan_1d', 'เตือนรอบแผนการรักษาล่วงหน้า 1 วัน', 'TreatmentPlanSession', 1, NULL, '09:00', false,
                     'Auto', 'treatment_plan.session', false, now())
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "automation_dispatches");

            migrationBuilder.DropTable(
                name: "report_exports");

            migrationBuilder.DropTable(
                name: "automation_rules");
        }
    }
}
