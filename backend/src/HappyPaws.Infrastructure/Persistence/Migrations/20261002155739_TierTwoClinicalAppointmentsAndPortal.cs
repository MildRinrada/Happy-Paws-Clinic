using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TierTwoClinicalAppointmentsAndPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "behaviour",
                table: "pets",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "diet",
                table: "pets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nickname",
                table: "pets",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_document_id",
                table: "pets",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_late_cancellation",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "series_id",
                table: "appointments",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type_code",
                table: "appointments",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "regular");

            migrationBuilder.CreateTable(
                name: "appointment_types",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    types = table.Column<string>(type: "jsonb", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appointment_types", x => x.id);
                    table.CheckConstraint("ck_appointment_types_singleton", "id = 'set_appointment_types'");
                });

            migrationBuilder.CreateTable(
                name: "cancellation_policy",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    cutoff_hours = table.Column<int>(type: "integer", nullable: false),
                    fee_satang = table.Column<long>(type: "bigint", nullable: false),
                    free_allowance = table.Column<int>(type: "integer", nullable: false),
                    window_days = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cancellation_policy", x => x.id);
                    table.CheckConstraint("ck_cancellation_policy_singleton", "id = 'set_cancellation_policy'");
                });

            migrationBuilder.CreateTable(
                name: "customer_packages",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    service_ids = table.Column<string[]>(type: "text[]", nullable: false),
                    total_uses = table.Column<int>(type: "integer", nullable: false),
                    used_count = table.Column<int>(type: "integer", nullable: false),
                    purchased_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expires_on = table.Column<DateOnly>(type: "date", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_packages", x => x.id);
                    table.CheckConstraint("ck_customer_packages_uses", "used_count >= 0 AND used_count <= total_uses");
                    table.ForeignKey(
                        name: "fk_customer_packages_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inbox_notifications",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    template_key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    reference_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    read_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_inbox_notifications_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lab_requests",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    consultation_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    destination = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    external_lab_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    priority = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    tests = table.Column<string>(type: "jsonb", nullable: false),
                    clinical_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    requested_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    resulted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    reviewed_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lab_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_lab_requests_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lab_requests_pet_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_requests",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    user_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_password_reset_requests_user_account_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pet_documents",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    added_by = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    file_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    declared_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    visible_to_owner = table.Column<bool>(type: "boolean", nullable: false),
                    consultation_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pet_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_pet_documents_pets_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pet_vaccinations",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    vaccine_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    manufacturer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    lot_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    vial_expires_on = table.Column<DateOnly>(type: "date", nullable: false),
                    administered_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    administered_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    consultation_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    next_due_on = table.Column<DateOnly>(type: "date", nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pet_vaccinations", x => x.id);
                    table.ForeignKey(
                        name: "fk_pet_vaccinations_pets_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "phone_verifications",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    phone_digits = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    purpose = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phone_verifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "treatment_plans",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    service_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    consultation_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    template_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    series_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    created_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    sessions = table.Column<string>(type: "jsonb", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_treatment_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_treatment_plans_pets_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "treatment_templates",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    species = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    procedures = table.Column<string>(type: "jsonb", nullable: false),
                    medicines = table.Column<string>(type: "jsonb", nullable: false),
                    plan = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    sessions = table.Column<int>(type: "integer", nullable: true),
                    session_interval_days = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_treatment_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "waiting_list",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    service_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    earliest_on = table.Column<DateOnly>(type: "date", nullable: false),
                    latest_on = table.Column<DateOnly>(type: "date", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    added_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    last_notified_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    notified_count = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waiting_list", x => x.id);
                    table.ForeignKey(
                        name: "fk_waiting_list_clinic_service_service_id",
                        column: x => x.service_id,
                        principalTable: "clinic_services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_waiting_list_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_waiting_list_pet_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lab_results",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    lab_request_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    analyte = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value_numeric = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    value_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    reference_low = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    reference_high = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    reference_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    flag = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    recorded_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lab_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_lab_results_lab_requests_lab_request_id",
                        column: x => x.lab_request_id,
                        principalTable: "lab_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_series_id",
                table: "appointments",
                column: "series_id",
                filter: "series_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_customer_packages_customer_id",
                table: "customer_packages",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_inbox_notifications_customer_id",
                table: "inbox_notifications",
                column: "customer_id",
                filter: "read_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_inbox_notifications_customer_id_created_at",
                table: "inbox_notifications",
                columns: new[] { "customer_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_lab_requests_branch_id_status_requested_at",
                table: "lab_requests",
                columns: new[] { "branch_id", "status", "requested_at" });

            migrationBuilder.CreateIndex(
                name: "ix_lab_requests_pet_id_requested_at",
                table: "lab_requests",
                columns: new[] { "pet_id", "requested_at" });

            migrationBuilder.CreateIndex(
                name: "ix_lab_results_lab_request_id",
                table: "lab_results",
                column: "lab_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_requests_token_hash",
                table: "password_reset_requests",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_requests_user_id_created_at",
                table: "password_reset_requests",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_pet_documents_pet_id_created_at",
                table: "pet_documents",
                columns: new[] { "pet_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_pet_documents_storage_key",
                table: "pet_documents",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pet_vaccinations_lot_number",
                table: "pet_vaccinations",
                column: "lot_number");

            migrationBuilder.CreateIndex(
                name: "ix_pet_vaccinations_next_due_on",
                table: "pet_vaccinations",
                column: "next_due_on",
                filter: "next_due_on IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pet_vaccinations_pet_id_administered_at",
                table: "pet_vaccinations",
                columns: new[] { "pet_id", "administered_at" });

            migrationBuilder.CreateIndex(
                name: "ix_phone_verifications_phone_digits_created_at",
                table: "phone_verifications",
                columns: new[] { "phone_digits", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_treatment_plans_pet_id",
                table: "treatment_plans",
                column: "pet_id");

            migrationBuilder.CreateIndex(
                name: "ix_treatment_templates_name",
                table: "treatment_templates",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_waiting_list_customer_id",
                table: "waiting_list",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_waiting_list_pet_id_service_id",
                table: "waiting_list",
                columns: new[] { "pet_id", "service_id" });

            migrationBuilder.CreateIndex(
                name: "ix_waiting_list_service_id_latest_on",
                table: "waiting_list",
                columns: new[] { "service_id", "latest_on" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointment_types");

            migrationBuilder.DropTable(
                name: "cancellation_policy");

            migrationBuilder.DropTable(
                name: "customer_packages");

            migrationBuilder.DropTable(
                name: "inbox_notifications");

            migrationBuilder.DropTable(
                name: "lab_results");

            migrationBuilder.DropTable(
                name: "password_reset_requests");

            migrationBuilder.DropTable(
                name: "pet_documents");

            migrationBuilder.DropTable(
                name: "pet_vaccinations");

            migrationBuilder.DropTable(
                name: "phone_verifications");

            migrationBuilder.DropTable(
                name: "treatment_plans");

            migrationBuilder.DropTable(
                name: "treatment_templates");

            migrationBuilder.DropTable(
                name: "waiting_list");

            migrationBuilder.DropTable(
                name: "lab_requests");

            migrationBuilder.DropIndex(
                name: "ix_appointments_series_id",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "behaviour",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "diet",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "nickname",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "photo_document_id",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "is_late_cancellation",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "series_id",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "type_code",
                table: "appointments");
        }
    }
}
