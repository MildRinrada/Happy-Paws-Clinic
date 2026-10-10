using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StaffAiAssistant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staff_ai_daily_briefs",
                columns: table => new
                {
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    brief_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    numbers = table.Column<string>(type: "jsonb", nullable: true),
                    text = table.Column<string>(type: "text", nullable: true),
                    claimed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_ai_daily_briefs", x => new { x.branch_id, x.brief_date });
                    table.ForeignKey(
                        name: "fk_staff_ai_daily_briefs_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff_ai_pet_summaries",
                columns: table => new
                {
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    citations = table.Column<string>(type: "jsonb", nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_ai_pet_summaries", x => x.pet_id);
                    table.ForeignKey(
                        name: "fk_staff_ai_pet_summaries_pets_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "staff_ai_usage",
                columns: table => new
                {
                    month = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    feature = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    used = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_ai_usage", x => new { x.month, x.feature });
                    table.CheckConstraint("ck_staff_ai_usage_used_not_negative", "used >= 0");
                });

            // ai:use is the second scope outside the contract's catalogue (docs/contract-extensions.md
            // § 34). The veterinarian and the receptionist get it by default, as the starter roles
            // now list it. clinic_admin does not: the owner approved it for vets, the counter and the
            // owner only. The owner holds * and needs nothing. Only the system roles are touched; an
            // admin can take it away again, or add it to any role.
            migrationBuilder.Sql(
                "UPDATE roles SET scopes = array_append(scopes, 'ai:use') " +
                "WHERE name IN ('veterinarian', 'receptionist') AND is_system " +
                "AND NOT ('ai:use' = ANY (scopes));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE roles SET scopes = array_remove(scopes, 'ai:use');");

            migrationBuilder.DropTable(
                name: "staff_ai_daily_briefs");

            migrationBuilder.DropTable(
                name: "staff_ai_pet_summaries");

            migrationBuilder.DropTable(
                name: "staff_ai_usage");
        }
    }
}
