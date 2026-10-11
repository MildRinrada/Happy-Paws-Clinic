using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StarterDashboardAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // dashboard:read on the veterinarian and the receptionist is beyond the contract's
            // starter table (docs/contract-extensions.md § 46): the overview page is where their
            // day starts. The starter roles now list it; this gives the same to clinics seeded
            // before. Only the system roles are touched, and an admin can take it away again.
            migrationBuilder.Sql(
                "UPDATE roles SET scopes = array_append(scopes, 'dashboard:read') " +
                "WHERE name IN ('veterinarian', 'receptionist') AND is_system " +
                "AND NOT ('dashboard:read' = ANY (scopes));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE roles SET scopes = array_remove(scopes, 'dashboard:read') " +
                "WHERE name IN ('veterinarian', 'receptionist') AND is_system;");
        }
    }
}
