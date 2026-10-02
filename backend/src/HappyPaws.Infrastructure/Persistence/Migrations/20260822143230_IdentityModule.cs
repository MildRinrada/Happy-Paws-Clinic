using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IdentityModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    scopes = table.Column<string[]>(type: "text[]", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    two_factor_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    two_factor_secret = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    credentials_invalidated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staff_memberships",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    user_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    staff_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    license_no = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    role_ids = table.Column<string[]>(type: "text[]", nullable: false),
                    branch_ids = table.Column<string[]>(type: "text[]", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_memberships", x => x.id);
                    table.ForeignKey(
                        name: "fk_staff_memberships_user_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    user_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    refresh_token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    revoked_for_reuse = table.Column<bool>(type: "boolean", nullable: false),
                    user_agent = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    two_factor_satisfied = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_sessions_user_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_staff_memberships_user_id",
                table: "staff_memberships",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_accounts_email",
                table: "user_accounts",
                column: "email",
                unique: true,
                filter: "email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_accounts_phone_number",
                table: "user_accounts",
                column: "phone_number",
                unique: true,
                filter: "phone_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_refresh_token_hash",
                table: "user_sessions",
                column: "refresh_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_user_id_revoked_at",
                table: "user_sessions",
                columns: new[] { "user_id", "revoked_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "staff_memberships");

            migrationBuilder.DropTable(
                name: "user_sessions");

            migrationBuilder.DropTable(
                name: "user_accounts");
        }
    }
}
