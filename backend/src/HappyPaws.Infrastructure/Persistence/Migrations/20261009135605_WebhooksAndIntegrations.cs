using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WebhooksAndIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "api_keys",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    secret_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scopes = table.Column<string[]>(type: "text[]", nullable: false),
                    branch_ids = table.Column<string[]>(type: "text[]", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "calendar_feeds",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    token_ciphertext = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_feeds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lab_provider_settings",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    account_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    base_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    api_key_ciphertext = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lab_provider_settings", x => x.id);
                    table.CheckConstraint("ck_lab_provider_settings_singleton", "id = 'set_lab'");
                });

            migrationBuilder.CreateTable(
                name: "payment_provider_settings",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    mode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    public_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    secret_key_ciphertext = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    webhook_secret_ciphertext = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_provider_settings", x => x.id);
                    table.CheckConstraint("ck_payment_provider_settings_singleton", "id = 'set_payment'");
                });

            migrationBuilder.CreateTable(
                name: "webhook_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    dispatched_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_subscriptions",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    events = table.Column<string[]>(type: "text[]", nullable: false),
                    secret_ciphertext = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_subscriptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_deliveries",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    subscription_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    event_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    event_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_attempt_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_status_code = table.Column<int>(type: "integer", nullable: true),
                    last_error = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    last_response_body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    last_duration_ms = table.Column<int>(type: "integer", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "fk_webhook_deliveries_webhook_event_event_id",
                        column: x => x.event_id,
                        principalTable: "webhook_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_webhook_deliveries_webhook_subscription_subscription_id",
                        column: x => x.subscription_id,
                        principalTable: "webhook_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_prefix",
                table: "api_keys",
                column: "prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_secret_hash",
                table: "api_keys",
                column: "secret_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_calendar_feeds_staff_id",
                table: "calendar_feeds",
                column: "staff_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_calendar_feeds_token_hash",
                table: "calendar_feeds",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_deliveries_due",
                table: "webhook_deliveries",
                column: "next_attempt_at",
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_deliveries_event_id_subscription_id",
                table: "webhook_deliveries",
                columns: new[] { "event_id", "subscription_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_deliveries_subscription_id_created_at_id",
                table: "webhook_deliveries",
                columns: new[] { "subscription_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_events_undispatched",
                table: "webhook_events",
                column: "occurred_at",
                filter: "dispatched_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_keys");

            migrationBuilder.DropTable(
                name: "calendar_feeds");

            migrationBuilder.DropTable(
                name: "lab_provider_settings");

            migrationBuilder.DropTable(
                name: "payment_provider_settings");

            migrationBuilder.DropTable(
                name: "webhook_deliveries");

            migrationBuilder.DropTable(
                name: "webhook_events");

            migrationBuilder.DropTable(
                name: "webhook_subscriptions");
        }
    }
}
