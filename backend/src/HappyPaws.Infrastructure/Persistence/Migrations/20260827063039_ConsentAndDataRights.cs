using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsentAndDataRights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_consents",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    purpose = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    granted = table.Column<bool>(type: "boolean", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    recorded_by_user_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    policy_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_consents", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_consents_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "data_subject_requests",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    raised_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    detail = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    closed_by_user_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    resolution = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_subject_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_data_subject_requests_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_consents_customer_id_purpose_recorded_at",
                table: "customer_consents",
                columns: new[] { "customer_id", "purpose", "recorded_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_data_subject_requests_customer_id",
                table: "data_subject_requests",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_data_subject_requests_status_raised_at",
                table: "data_subject_requests",
                columns: new[] { "status", "raised_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_consents");

            migrationBuilder.DropTable(
                name: "data_subject_requests");
        }
    }
}
