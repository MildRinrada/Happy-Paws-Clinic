using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StaffDepartmentsAndPublicProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bio",
                table: "staff_memberships",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "career_start_year",
                table: "staff_memberships",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "department",
                table: "staff_memberships",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "support");

            // Existing members start where their staff type points, as Departments.ForStaffType
            // would place a new one. Anyone else stays in support until the clinic moves them.
            migrationBuilder.Sql("""
                UPDATE staff_memberships SET department = CASE staff_type
                    WHEN 'veterinarian' THEN 'veterinary'
                    WHEN 'vet_assistant' THEN 'nursing'
                    WHEN 'receptionist' THEN 'front_desk'
                    WHEN 'cashier' THEN 'finance'
                    WHEN 'owner' THEN 'management'
                    WHEN 'clinic_admin' THEN 'management'
                    ELSE 'support'
                END;
                """);

            migrationBuilder.AddColumn<string>(
                name: "job_title",
                table: "staff_memberships",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_path",
                table: "staff_memberships",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "show_on_website",
                table: "staff_memberships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "specialty",
                table: "staff_memberships",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bio",
                table: "staff_memberships");

            migrationBuilder.DropColumn(
                name: "career_start_year",
                table: "staff_memberships");

            migrationBuilder.DropColumn(
                name: "department",
                table: "staff_memberships");

            migrationBuilder.DropColumn(
                name: "job_title",
                table: "staff_memberships");

            migrationBuilder.DropColumn(
                name: "photo_path",
                table: "staff_memberships");

            migrationBuilder.DropColumn(
                name: "show_on_website",
                table: "staff_memberships");

            migrationBuilder.DropColumn(
                name: "specialty",
                table: "staff_memberships");
        }
    }
}
