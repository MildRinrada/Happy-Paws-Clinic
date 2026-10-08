using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LineFollowersAndAutomationEdits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "automation_rules",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_by_staff_id",
                table: "automation_rules",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "line_followers",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    line_user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    picture_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    followed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    unfollowed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_line_followers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_line_followers_last_seen_at",
                table: "line_followers",
                column: "last_seen_at");

            migrationBuilder.CreateIndex(
                name: "ix_line_followers_line_user_id",
                table: "line_followers",
                column: "line_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "line_followers");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "automation_rules");

            migrationBuilder.DropColumn(
                name: "deleted_by_staff_id",
                table: "automation_rules");
        }
    }
}
