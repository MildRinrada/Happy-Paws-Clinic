using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClinicLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The clinic's own logo (docs/contract-extensions.md § 50). The logo itself stays in
            // logo_url; these are the optional square tab icon and the switch that hides the name
            // beside a logo that already spells it. The switch starts on, so a clinic that has
            // its row already looks exactly as it did.
            migrationBuilder.AddColumn<bool>(
                name: "show_name_with_logo",
                table: "clinic_profile",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "tab_icon_url",
                table: "clinic_profile",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "show_name_with_logo",
                table: "clinic_profile");

            migrationBuilder.DropColumn(
                name: "tab_icon_url",
                table: "clinic_profile");
        }
    }
}
