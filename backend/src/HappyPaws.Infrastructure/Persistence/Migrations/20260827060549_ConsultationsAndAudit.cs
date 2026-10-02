using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsultationsAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "left_reason",
                table: "queue_tickets",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "priority",
                table: "queue_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "priority_reason",
                table: "queue_tickets",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "priority_set_at",
                table: "queue_tickets",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "priority_set_by_staff_id",
                table: "queue_tickets",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audit_entries",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    resource_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    actor_user_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    actor_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    actor_customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    request_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    details_json = table.Column<string>(type: "jsonb", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "consultations",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    appointment_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    queue_ticket_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    vet_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    opened_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    medical_record_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    chief_complaint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    subjective = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    objective = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    assessment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    plan = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consultations", x => x.id);
                    table.ForeignKey(
                        name: "fk_consultations_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consultations_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consultations_pet_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consultations_queue_ticket_queue_ticket_id",
                        column: x => x.queue_ticket_id,
                        principalTable: "queue_tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pet_allergies",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    substance = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    reaction = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    recorded_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    withdrawn_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    withdrawn_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pet_allergies", x => x.id);
                    table.ForeignKey(
                        name: "fk_pet_allergies_pet_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "consultation_diagnoses",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    consultation_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consultation_diagnoses", x => x.id);
                    table.ForeignKey(
                        name: "fk_consultation_diagnoses_consultations_consultation_id",
                        column: x => x.consultation_id,
                        principalTable: "consultations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "consultation_findings",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    consultation_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    body_system = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    is_normal = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consultation_findings", x => x.id);
                    table.ForeignKey(
                        name: "fk_consultation_findings_consultations_consultation_id",
                        column: x => x.consultation_id,
                        principalTable: "consultations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "consultation_treatments",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    consultation_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    service_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price_satang = table.Column<long>(type: "bigint", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    performed_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    performed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consultation_treatments", x => x.id);
                    table.ForeignKey(
                        name: "fk_consultation_treatments_consultations_consultation_id",
                        column: x => x.consultation_id,
                        principalTable: "consultations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "consultation_vitals",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    consultation_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    recorded_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: true),
                    temperature_c = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    heart_rate_bpm = table.Column<int>(type: "integer", nullable: true),
                    respiratory_rate_bpm = table.Column<int>(type: "integer", nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consultation_vitals", x => x.id);
                    table.ForeignKey(
                        name: "fk_consultation_vitals_consultations_consultation_id",
                        column: x => x.consultation_id,
                        principalTable: "consultations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "medical_records",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    consultation_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    content_json = table.Column<string>(type: "text", nullable: false),
                    signed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    signed_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medical_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_medical_records_consultations_consultation_id",
                        column: x => x.consultation_id,
                        principalTable: "consultations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medical_records_pet_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prescriptions",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    consultation_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    prescribed_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    prescribed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    drug_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dose = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    frequency = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    instructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    allergy_acknowledgement = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    dispensed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    dispensed_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prescriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_prescriptions_consultations_consultation_id",
                        column: x => x.consultation_id,
                        principalTable: "consultations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_prescriptions_pets_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "medical_record_amendments",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    medical_record_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    content_json = table.Column<string>(type: "text", nullable: false),
                    changes_json = table.Column<string>(type: "jsonb", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medical_record_amendments", x => x.id);
                    table.ForeignKey(
                        name: "fk_medical_record_amendments_medical_record_medical_record_id",
                        column: x => x.medical_record_id,
                        principalTable: "medical_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_queue_tickets_branch_id_service_date_priority_skipped_at_se",
                table: "queue_tickets",
                columns: new[] { "branch_id", "service_date", "priority", "skipped_at", "sequence" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_action_occurred_at",
                table: "audit_entries",
                columns: new[] { "action", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_actor_user_id_occurred_at",
                table: "audit_entries",
                columns: new[] { "actor_user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_occurred_at",
                table: "audit_entries",
                column: "occurred_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_pet_id_occurred_at",
                table: "audit_entries",
                columns: new[] { "pet_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_resource_type_resource_id_occurred_at",
                table: "audit_entries",
                columns: new[] { "resource_type", "resource_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_consultation_diagnoses_code",
                table: "consultation_diagnoses",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "ux_consultation_primary_diagnosis",
                table: "consultation_diagnoses",
                column: "consultation_id",
                unique: true,
                filter: "is_primary");

            migrationBuilder.CreateIndex(
                name: "ix_consultation_findings_consultation_id",
                table: "consultation_findings",
                column: "consultation_id");

            migrationBuilder.CreateIndex(
                name: "ix_consultation_treatments_consultation_id",
                table: "consultation_treatments",
                column: "consultation_id");

            migrationBuilder.CreateIndex(
                name: "ix_consultation_vitals_consultation_id",
                table: "consultation_vitals",
                column: "consultation_id");

            migrationBuilder.CreateIndex(
                name: "ix_consultation_vitals_pet_id_recorded_at",
                table: "consultation_vitals",
                columns: new[] { "pet_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_consultations_appointment_id",
                table: "consultations",
                column: "appointment_id",
                unique: true,
                filter: "appointment_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_consultations_branch_id_status",
                table: "consultations",
                columns: new[] { "branch_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_consultations_pet_id_opened_at",
                table: "consultations",
                columns: new[] { "pet_id", "opened_at" });

            migrationBuilder.CreateIndex(
                name: "ix_consultations_queue_ticket_id",
                table: "consultations",
                column: "queue_ticket_id",
                unique: true,
                filter: "queue_ticket_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_consultations_vet_staff_id_opened_at",
                table: "consultations",
                columns: new[] { "vet_staff_id", "opened_at" });

            migrationBuilder.CreateIndex(
                name: "ix_medical_record_amendments_medical_record_id_created_at",
                table: "medical_record_amendments",
                columns: new[] { "medical_record_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_medical_records_consultation_id",
                table: "medical_records",
                column: "consultation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_medical_records_pet_id_signed_at",
                table: "medical_records",
                columns: new[] { "pet_id", "signed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_pet_allergies_pet_id",
                table: "pet_allergies",
                column: "pet_id",
                filter: "withdrawn_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pet_allergies_pet_id_substance",
                table: "pet_allergies",
                columns: new[] { "pet_id", "substance" },
                unique: true,
                filter: "withdrawn_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_prescriptions_consultation_id",
                table: "prescriptions",
                column: "consultation_id");

            migrationBuilder.CreateIndex(
                name: "ix_prescriptions_dispensed_at",
                table: "prescriptions",
                column: "dispensed_at",
                filter: "dispensed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_prescriptions_pet_id_prescribed_at",
                table: "prescriptions",
                columns: new[] { "pet_id", "prescribed_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_entries");

            migrationBuilder.DropTable(
                name: "consultation_diagnoses");

            migrationBuilder.DropTable(
                name: "consultation_findings");

            migrationBuilder.DropTable(
                name: "consultation_treatments");

            migrationBuilder.DropTable(
                name: "consultation_vitals");

            migrationBuilder.DropTable(
                name: "medical_record_amendments");

            migrationBuilder.DropTable(
                name: "pet_allergies");

            migrationBuilder.DropTable(
                name: "prescriptions");

            migrationBuilder.DropTable(
                name: "medical_records");

            migrationBuilder.DropTable(
                name: "consultations");

            migrationBuilder.DropIndex(
                name: "ix_queue_tickets_branch_id_service_date_priority_skipped_at_se",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "left_reason",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "priority_reason",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "priority_set_at",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "priority_set_by_staff_id",
                table: "queue_tickets");
        }
    }
}
