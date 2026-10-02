using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClinicSetupStaffAndServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "booking_windows",
                table: "clinic_services",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'");

            migrationBuilder.AddColumn<string[]>(
                name: "branch_ids",
                table: "clinic_services",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<string>(
                name: "price_schedule",
                table: "clinic_services",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'");

            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "branches",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "latitude",
                table: "branches",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitude",
                table: "branches",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "clinic_holidays",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clinic_holidays", x => x.id);
                    table.ForeignKey(
                        name: "fk_clinic_holidays_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "clinic_profile",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    logo_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    brand_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    license_no = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    operator_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clinic_profile", x => x.id);
                    table.CheckConstraint("ck_clinic_profile_singleton", "id = 'set_clinic'");
                });

            migrationBuilder.CreateTable(
                name: "medical_record_settings",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    required_fields = table.Column<string[]>(type: "text[]", nullable: false),
                    examination_systems = table.Column<string[]>(type: "text[]", nullable: false),
                    owner_visible_sections = table.Column<string[]>(type: "text[]", nullable: false),
                    signer_must_be_case_vet = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medical_record_settings", x => x.id);
                    table.CheckConstraint("ck_medical_record_settings_singleton", "id = 'set_medical_record'");
                });

            migrationBuilder.CreateTable(
                name: "opening_hours",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    days = table.Column<string>(type: "jsonb", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opening_hours", x => x.id);
                    table.CheckConstraint("ck_opening_hours_singleton", "id = 'set_hours'");
                });

            migrationBuilder.CreateTable(
                name: "queue_settings",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    number_prefix = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    number_digits = table.Column<int>(type: "integer", nullable: false),
                    reset_daily = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_queue_settings", x => x.id);
                    table.CheckConstraint("ck_queue_settings_singleton", "id = 'set_queue'");
                });

            migrationBuilder.CreateTable(
                name: "staff_invitations",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    staff_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    license_no = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    role_ids = table.Column<string[]>(type: "text[]", nullable: false),
                    branch_ids = table.Column<string[]>(type: "text[]", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    invited_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    accepted_user_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    pending_two_factor_secret = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_invitations", x => x.id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_branches_coordinates_paired",
                table: "branches",
                sql: "(latitude IS NULL) = (longitude IS NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_clinic_holidays_branch_id",
                table: "clinic_holidays",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_clinic_holidays_date_branch_id",
                table: "clinic_holidays",
                columns: new[] { "date", "branch_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_staff_invitations_accepted_at_created_at",
                table: "staff_invitations",
                columns: new[] { "accepted_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_staff_invitations_email",
                table: "staff_invitations",
                column: "email",
                unique: true,
                filter: "accepted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clinic_holidays");

            migrationBuilder.DropTable(
                name: "clinic_profile");

            migrationBuilder.DropTable(
                name: "medical_record_settings");

            migrationBuilder.DropTable(
                name: "opening_hours");

            migrationBuilder.DropTable(
                name: "queue_settings");

            migrationBuilder.DropTable(
                name: "staff_invitations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_branches_coordinates_paired",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "booking_windows",
                table: "clinic_services");

            migrationBuilder.DropColumn(
                name: "branch_ids",
                table: "clinic_services");

            migrationBuilder.DropColumn(
                name: "price_schedule",
                table: "clinic_services");

            migrationBuilder.DropColumn(
                name: "address",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "branches");
        }
    }
}
