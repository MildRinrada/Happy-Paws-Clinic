using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReferAFriend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "referral_enabled",
                table: "loyalty_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "referral_friend_points",
                table: "loyalty_settings",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "referral_monthly_cap",
                table: "loyalty_settings",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "referral_referrer_points",
                table: "loyalty_settings",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<string>(
                name: "referral_id",
                table: "loyalty_entries",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "referral_codes",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_referral_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_referral_codes_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "referrals",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    referrer_customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    friend_customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    reject_reason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    rejected_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    referrer_points = table.Column<int>(type: "integer", nullable: false),
                    friend_points = table.Column<int>(type: "integer", nullable: false),
                    over_monthly_cap = table.Column<bool>(type: "boolean", nullable: false),
                    reward_invoice_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    rewarded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_referrals", x => x.id);
                    table.CheckConstraint("ck_referrals_not_self_reward", "status <> 'Rewarded' OR referrer_customer_id <> friend_customer_id");
                    table.CheckConstraint("ck_referrals_points", "referrer_points >= 0 AND friend_points >= 0");
                    table.CheckConstraint("ck_referrals_status", "(status = 'Pending' AND reject_reason IS NULL AND rewarded_at IS NULL AND rejected_at IS NULL) OR (status = 'Rewarded' AND reject_reason IS NULL AND rewarded_at IS NOT NULL AND reward_invoice_id IS NOT NULL) OR (status = 'Rejected' AND reject_reason IS NOT NULL AND rejected_at IS NOT NULL AND rewarded_at IS NULL)");
                    table.ForeignKey(
                        name: "fk_referrals_customers_friend_customer_id",
                        column: x => x.friend_customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_referrals_customers_referrer_customer_id",
                        column: x => x.referrer_customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_referrals_invoices_reward_invoice_id",
                        column: x => x.reward_invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_loyalty_settings_referral",
                table: "loyalty_settings",
                sql: "referral_referrer_points >= 0 AND referral_friend_points >= 0 AND referral_monthly_cap > 0");

            migrationBuilder.CreateIndex(
                name: "ux_loyalty_entries_referral_per_customer",
                table: "loyalty_entries",
                columns: new[] { "referral_id", "customer_id" },
                unique: true,
                filter: "kind = 'Referral'");

            migrationBuilder.CreateIndex(
                name: "ux_referral_codes_code",
                table: "referral_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_referral_codes_customer",
                table: "referral_codes",
                column: "customer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_referrals_created_at_id",
                table: "referrals",
                columns: new[] { "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_referrals_referrer_customer_id_rewarded_at",
                table: "referrals",
                columns: new[] { "referrer_customer_id", "rewarded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_referrals_reward_invoice_id",
                table: "referrals",
                column: "reward_invoice_id",
                unique: true,
                filter: "reward_invoice_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_referrals_friend",
                table: "referrals",
                column: "friend_customer_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Referral points stay in customers' balances: the rows become hand adjustments, the
            // kind the code before this migration knows, with their note kept
            // (docs/contract-extensions.md § 39).
            migrationBuilder.Sql("UPDATE loyalty_entries SET kind = 'Adjust' WHERE kind = 'Referral';");

            migrationBuilder.DropTable(
                name: "referral_codes");

            migrationBuilder.DropTable(
                name: "referrals");

            migrationBuilder.DropCheckConstraint(
                name: "ck_loyalty_settings_referral",
                table: "loyalty_settings");

            migrationBuilder.DropIndex(
                name: "ux_loyalty_entries_referral_per_customer",
                table: "loyalty_entries");

            migrationBuilder.DropColumn(
                name: "referral_enabled",
                table: "loyalty_settings");

            migrationBuilder.DropColumn(
                name: "referral_friend_points",
                table: "loyalty_settings");

            migrationBuilder.DropColumn(
                name: "referral_monthly_cap",
                table: "loyalty_settings");

            migrationBuilder.DropColumn(
                name: "referral_referrer_points",
                table: "loyalty_settings");

            migrationBuilder.DropColumn(
                name: "referral_id",
                table: "loyalty_entries");
        }
    }
}
