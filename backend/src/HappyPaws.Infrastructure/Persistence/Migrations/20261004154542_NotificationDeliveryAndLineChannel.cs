using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NotificationDeliveryAndLineChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "line_unfollowed_at",
                table: "customers",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "line_user_id",
                table: "customers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "line_channel_settings",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    channel_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    channel_secret = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    channel_access_token = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    disabled_templates = table.Column<string[]>(type: "text[]", nullable: false),
                    broadcast_monthly_limit = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_line_channel_settings", x => x.id);
                    table.CheckConstraint("ck_line_channel_settings_singleton", "id = 'set_line'");
                });

            migrationBuilder.CreateTable(
                name: "notification_deliveries",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    template_key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    channel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    recipient = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    error_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    fallback_from = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    source = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    batch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    sent_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_deliveries_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customers_line_user_id",
                table: "customers",
                column: "line_user_id",
                unique: true,
                filter: "line_user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_batch_id",
                table: "notification_deliveries",
                column: "batch_id",
                filter: "batch_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_broadcasts_sent",
                table: "notification_deliveries",
                column: "created_at",
                filter: "source = 'Broadcast' AND status = 'Sent'");

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_created_at_id",
                table: "notification_deliveries",
                columns: new[] { "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_customer_id_created_at",
                table: "notification_deliveries",
                columns: new[] { "customer_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "line_channel_settings");

            migrationBuilder.DropTable(
                name: "notification_deliveries");

            migrationBuilder.DropIndex(
                name: "ix_customers_line_user_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "line_unfollowed_at",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "line_user_id",
                table: "customers");
        }
    }
}
