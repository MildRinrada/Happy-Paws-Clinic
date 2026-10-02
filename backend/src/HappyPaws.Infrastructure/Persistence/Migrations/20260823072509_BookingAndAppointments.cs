using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BookingAndAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "booking_settings",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    slot_minutes = table.Column<int>(type: "integer", nullable: false),
                    lead_time_minutes = table.Column<int>(type: "integer", nullable: false),
                    cancellation_cutoff_hours = table.Column<int>(type: "integer", nullable: false),
                    max_advance_days = table.Column<int>(type: "integer", nullable: false),
                    walk_in_slots_per_hour = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_settings", x => x.id);
                    table.CheckConstraint("ck_booking_settings_singleton", "id = 'set_booking'");
                });

            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clinic_services",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    price_satang = table.Column<long>(type: "bigint", nullable: false),
                    online_booking_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    eligible_staff_ids = table.Column<string[]>(type: "text[]", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clinic_services", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    user_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "slot_holds",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    service_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    held_by_customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    released_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_slot_holds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staff_shifts",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_shifts", x => x.id);
                    table.ForeignKey(
                        name: "fk_staff_shifts_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_staff_shifts_staff_membership_staff_id",
                        column: x => x.staff_id,
                        principalTable: "staff_memberships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pets",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    hospital_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    species = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    breed = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    primary_customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    deceased_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pets", x => x.id);
                    table.ForeignKey(
                        name: "fk_pets_customers_primary_customer_id",
                        column: x => x.primary_customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    service_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_walk_in = table.Column<bool>(type: "boolean", nullable: false),
                    booked_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    source_hold_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    checked_in_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cancelled_by_user_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appointments", x => x.id);
                    table.ForeignKey(
                        name: "fk_appointments_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_appointments_clinic_service_service_id",
                        column: x => x.service_id,
                        principalTable: "clinic_services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_appointments_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_appointments_pet_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pet_ownerships",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    may_view_history = table.Column<bool>(type: "boolean", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pet_ownerships", x => x.id);
                    table.ForeignKey(
                        name: "fk_pet_ownerships_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pet_ownerships_pets_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "queue_tickets",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    service_date = table.Column<DateOnly>(type: "date", nullable: false),
                    appointment_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    service_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_queue_tickets", x => x.id);
                    table.ForeignKey(
                        name: "fk_queue_tickets_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_queue_tickets_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_branch_id_staff_id_starts_at",
                table: "appointments",
                columns: new[] { "branch_id", "staff_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_branch_id_starts_at_status",
                table: "appointments",
                columns: new[] { "branch_id", "starts_at", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_customer_id_starts_at",
                table: "appointments",
                columns: new[] { "customer_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_pet_id_starts_at",
                table: "appointments",
                columns: new[] { "pet_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_service_id",
                table: "appointments",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_branches_code",
                table: "branches",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_clinic_services_code",
                table: "clinic_services",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_clinic_services_online_booking_enabled_is_active",
                table: "clinic_services",
                columns: new[] { "online_booking_enabled", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_phone",
                table: "customers",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "ix_customers_user_id",
                table: "customers",
                column: "user_id",
                unique: true,
                filter: "user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pet_ownerships_customer_id_revoked_at",
                table: "pet_ownerships",
                columns: new[] { "customer_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_pet_ownerships_pet_id_customer_id",
                table: "pet_ownerships",
                columns: new[] { "pet_id", "customer_id" },
                unique: true,
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pets_hospital_number",
                table: "pets",
                column: "hospital_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pets_primary_customer_id",
                table: "pets",
                column: "primary_customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_queue_tickets_appointment_id",
                table: "queue_tickets",
                column: "appointment_id",
                unique: true,
                filter: "appointment_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_queue_tickets_branch_id_service_date_number",
                table: "queue_tickets",
                columns: new[] { "branch_id", "service_date", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_slot_holds_branch_id_staff_id_starts_at",
                table: "slot_holds",
                columns: new[] { "branch_id", "staff_id", "starts_at" },
                filter: "consumed_at IS NULL AND released_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_slot_holds_expires_at",
                table: "slot_holds",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_staff_shifts_branch_id_starts_at_ends_at",
                table: "staff_shifts",
                columns: new[] { "branch_id", "starts_at", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "ix_staff_shifts_staff_id_starts_at",
                table: "staff_shifts",
                columns: new[] { "staff_id", "starts_at" });

            // The clinic's booking rules are a singleton row, so it is seeded here rather than
            // left to whoever first opens the settings screen. Availability, lead time and the
            // change cut-off all read it, and a clinic with no row would be answering booking
            // requests from defaults nobody chose.
            //
            // ON CONFLICT DO NOTHING so re-running against a database that already has the row
            // (a restore, a re-applied migration) is not an error. The values match
            // BookingSettings.CreateDefault().
            migrationBuilder.Sql(
                """
                INSERT INTO booking_settings
                    (id, slot_minutes, lead_time_minutes, cancellation_cutoff_hours,
                     max_advance_days, walk_in_slots_per_hour, created_at)
                VALUES
                    ('set_booking', 30, 120, 12, 60, 0, now())
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_settings");

            migrationBuilder.DropTable(
                name: "pet_ownerships");

            migrationBuilder.DropTable(
                name: "queue_tickets");

            migrationBuilder.DropTable(
                name: "slot_holds");

            migrationBuilder.DropTable(
                name: "staff_shifts");

            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropTable(
                name: "clinic_services");

            migrationBuilder.DropTable(
                name: "pets");

            migrationBuilder.DropTable(
                name: "customers");
        }
    }
}
