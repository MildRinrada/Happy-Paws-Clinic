using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ManageCrmAndInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cancel_reason",
                table: "purchase_orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "purchase_orders",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancelled_by_staff_id",
                table: "purchase_orders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "loyalty_entries",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "satang_per_point",
                table: "loyalty_entries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "retired_at",
                table: "inventory_items",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deactivated_at",
                table: "coupons",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deactivated_by_staff_id",
                table: "coupons",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deactivation_reason",
                table: "coupons",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "loyalty_settings",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    satang_per_point = table.Column<long>(type: "bigint", nullable: false),
                    point_value_satang = table.Column<long>(type: "bigint", nullable: false),
                    silver_min_points = table.Column<int>(type: "integer", nullable: false),
                    gold_min_points = table.Column<int>(type: "integer", nullable: false),
                    points_expire_after_months = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loyalty_settings", x => x.id);
                    table.CheckConstraint("ck_loyalty_settings_singleton", "id = 'set_loyalty'");
                    table.CheckConstraint("ck_loyalty_settings_values", "satang_per_point > 0 AND point_value_satang > 0 AND silver_min_points > 0 AND gold_min_points > silver_min_points AND (points_expire_after_months IS NULL OR points_expire_after_months > 0)");
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_ordered_at_id",
                table: "purchase_orders",
                columns: new[] { "ordered_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_coupons_created_at_id",
                table: "coupons",
                columns: new[] { "created_at", "id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_coupons_deactivated_by",
                table: "coupons",
                sql: "(deactivated_at IS NULL) = (deactivated_by_staff_id IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loyalty_settings");

            migrationBuilder.DropIndex(
                name: "ix_purchase_orders_ordered_at_id",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_coupons_created_at_id",
                table: "coupons");

            migrationBuilder.DropCheckConstraint(
                name: "ck_coupons_deactivated_by",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "cancel_reason",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "cancelled_by_staff_id",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "loyalty_entries");

            migrationBuilder.DropColumn(
                name: "satang_per_point",
                table: "loyalty_entries");

            migrationBuilder.DropColumn(
                name: "retired_at",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "deactivated_at",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "deactivated_by_staff_id",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "deactivation_reason",
                table: "coupons");
        }
    }
}
