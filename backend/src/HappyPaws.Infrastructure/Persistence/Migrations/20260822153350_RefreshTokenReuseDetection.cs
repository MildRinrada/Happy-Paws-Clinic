using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokenReuseDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "previous_refresh_token_hash",
                table: "user_sessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_previous_refresh_token_hash",
                table: "user_sessions",
                column: "previous_refresh_token_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_sessions_previous_refresh_token_hash",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "previous_refresh_token_hash",
                table: "user_sessions");
        }
    }
}
