using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomersAndPatients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<string>(
                name: "microchip_number",
                table: "pets",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "merged_into_customer_id",
                table: "customers",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customer_invitations",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    sent_to = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    invited_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_invitations", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_invitations_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pets_created_at",
                table: "pets",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_pets_microchip_number",
                table: "pets",
                column: "microchip_number",
                unique: true,
                filter: "microchip_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pets_name_trgm",
                table: "pets",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_display_name_trgm",
                table: "customers",
                column: "display_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_merged_into_customer_id",
                table: "customers",
                column: "merged_into_customer_id",
                filter: "merged_into_customer_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_customer_invitations_customer_id",
                table: "customer_invitations",
                column: "customer_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_invitations");

            migrationBuilder.DropIndex(
                name: "ix_pets_created_at",
                table: "pets");

            migrationBuilder.DropIndex(
                name: "ix_pets_microchip_number",
                table: "pets");

            migrationBuilder.DropIndex(
                name: "ix_pets_name_trgm",
                table: "pets");

            migrationBuilder.DropIndex(
                name: "ix_customers_display_name_trgm",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_merged_into_customer_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "microchip_number",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "merged_into_customer_id",
                table: "customers");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
