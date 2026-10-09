using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LoyaltyPackagesAndPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "activated_at",
                table: "customer_packages",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "branch_id",
                table: "customer_packages",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancel_reason",
                table: "customer_packages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "customer_packages",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invoice_id",
                table: "customer_packages",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "price_satang",
                table: "customer_packages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "sold_by_staff_id",
                table: "customer_packages",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            // Packages written before they were sold through the API had no bill to wait for; they
            // were usable from the moment they were recorded, and stay usable.
            migrationBuilder.Sql("UPDATE customer_packages SET activated_at = purchased_at WHERE activated_at IS NULL;");

            migrationBuilder.CreateTable(
                name: "coupons",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    percent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    amount_satang = table.Column<long>(type: "bigint", nullable: true),
                    service_ids = table.Column<string[]>(type: "text[]", nullable: false),
                    max_redemptions = table.Column<int>(type: "integer", nullable: true),
                    per_customer_limit = table.Column<int>(type: "integer", nullable: true),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: true),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    created_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    redeemed_count = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coupons", x => x.id);
                    table.CheckConstraint("ck_coupons_one_kind", "(percent IS NULL) <> (amount_satang IS NULL)");
                    table.CheckConstraint("ck_coupons_redeemed", "redeemed_count >= 0 AND (max_redemptions IS NULL OR redeemed_count <= max_redemptions)");
                    table.ForeignKey(
                        name: "fk_coupons_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_segments",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    species = table.Column<string[]>(type: "text[]", nullable: false),
                    inactive_days = table.Column<int>(type: "integer", nullable: true),
                    visited_within_days = table.Column<int>(type: "integer", nullable: true),
                    min_spend_satang = table.Column<long>(type: "bigint", nullable: true),
                    tiers = table.Column<string[]>(type: "text[]", nullable: false),
                    has_active_package = table.Column<bool>(type: "boolean", nullable: true),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    created_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_count = table.Column<int>(type: "integer", nullable: false),
                    reachable_count = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_segments", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_segments_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loyalty_entries",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    invoice_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    discount_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loyalty_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_loyalty_entries_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loyalty_entries_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "package_uses",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    package_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    invoice_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    invoice_line_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    service_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    released_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_package_uses", x => x.id);
                    table.CheckConstraint("ck_package_uses_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_package_uses_customer_packages_package_id",
                        column: x => x.package_id,
                        principalTable: "customer_packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_package_uses_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "coupon_redemptions",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    coupon_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    invoice_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    discount_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    amount_satang = table.Column<long>(type: "bigint", nullable: false),
                    redeemed_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    redeemed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    released_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coupon_redemptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_coupon_redemptions_coupons_coupon_id",
                        column: x => x.coupon_id,
                        principalTable: "coupons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_coupon_redemptions_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_coupon_redemptions_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_packages_branch_id",
                table: "customer_packages",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_packages_invoice_id",
                table: "customer_packages",
                column: "invoice_id",
                filter: "invoice_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_packages_price",
                table: "customer_packages",
                sql: "price_satang >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_coupon_redemptions_coupon_id_customer_id",
                table: "coupon_redemptions",
                columns: new[] { "coupon_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_coupon_redemptions_coupon_id_invoice_id",
                table: "coupon_redemptions",
                columns: new[] { "coupon_id", "invoice_id" },
                unique: true,
                filter: "released_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_coupon_redemptions_customer_id",
                table: "coupon_redemptions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_coupon_redemptions_invoice_id",
                table: "coupon_redemptions",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_coupons_branch_id",
                table: "coupons",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ux_coupons_code",
                table: "coupons",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_segments_branch_id",
                table: "customer_segments",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_entries_customer_id_occurred_at",
                table: "loyalty_entries",
                columns: new[] { "customer_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ux_loyalty_entries_earn_per_invoice",
                table: "loyalty_entries",
                column: "invoice_id",
                unique: true,
                filter: "kind = 'Earn'");

            migrationBuilder.CreateIndex(
                name: "ix_package_uses_invoice_id",
                table: "package_uses",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_package_uses_package_id",
                table: "package_uses",
                column: "package_id");

            migrationBuilder.AddForeignKey(
                name: "fk_customer_packages_branches_branch_id",
                table: "customer_packages",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_packages_invoices_invoice_id",
                table: "customer_packages",
                column: "invoice_id",
                principalTable: "invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customer_packages_branches_branch_id",
                table: "customer_packages");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_packages_invoices_invoice_id",
                table: "customer_packages");

            migrationBuilder.DropTable(
                name: "coupon_redemptions");

            migrationBuilder.DropTable(
                name: "customer_segments");

            migrationBuilder.DropTable(
                name: "loyalty_entries");

            migrationBuilder.DropTable(
                name: "package_uses");

            migrationBuilder.DropTable(
                name: "coupons");

            migrationBuilder.DropIndex(
                name: "ix_customer_packages_branch_id",
                table: "customer_packages");

            migrationBuilder.DropIndex(
                name: "ix_customer_packages_invoice_id",
                table: "customer_packages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_packages_price",
                table: "customer_packages");

            migrationBuilder.DropColumn(
                name: "activated_at",
                table: "customer_packages");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "customer_packages");

            migrationBuilder.DropColumn(
                name: "cancel_reason",
                table: "customer_packages");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "customer_packages");

            migrationBuilder.DropColumn(
                name: "invoice_id",
                table: "customer_packages");

            migrationBuilder.DropColumn(
                name: "price_satang",
                table: "customer_packages");

            migrationBuilder.DropColumn(
                name: "sold_by_staff_id",
                table: "customer_packages");
        }
    }
}
