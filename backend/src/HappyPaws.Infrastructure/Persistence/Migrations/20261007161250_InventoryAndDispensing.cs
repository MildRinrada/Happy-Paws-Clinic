using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InventoryAndDispensing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "charged_unit_price_satang",
                table: "prescriptions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inventory_item_id",
                table: "prescriptions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "stock_deducted",
                table: "prescriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "inventory_items",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    unit_price_satang = table.Column<long>(type: "bigint", nullable: false),
                    reorder_level = table.Column<int>(type: "integer", nullable: false),
                    created_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_items", x => x.id);
                    table.CheckConstraint("ck_inventory_items_price_not_negative", "unit_price_satang >= 0");
                    table.CheckConstraint("ck_inventory_items_reorder_not_negative", "reorder_level >= 0");
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    expected_on = table.Column<DateOnly>(type: "date", nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ordered_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ordered_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_orders_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_lots",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    item_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    lot_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    expires_on = table.Column<DateOnly>(type: "date", nullable: true),
                    quantity_on_hand = table.Column<int>(type: "integer", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_lots", x => x.id);
                    table.CheckConstraint("ck_stock_lots_quantity_not_negative", "quantity_on_hand >= 0");
                    table.ForeignKey(
                        name: "fk_stock_lots_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_lots_inventory_items_item_id",
                        column: x => x.item_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_lines",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    purchase_order_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    item_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_cost_satang = table.Column<long>(type: "bigint", nullable: false),
                    received_quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_order_lines", x => x.id);
                    table.CheckConstraint("ck_purchase_order_lines_quantity_positive", "quantity > 0");
                    table.CheckConstraint("ck_purchase_order_lines_received_within_ordered", "received_quantity >= 0 AND received_quantity <= quantity");
                    table.ForeignKey(
                        name: "fk_purchase_order_lines_inventory_items_item_id",
                        column: x => x.item_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_order_lines_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    item_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    lot_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    balance_after = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    recorded_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    unit_cost_satang = table.Column<long>(type: "bigint", nullable: true),
                    purchase_order_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    prescription_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    transfer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_movements_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_inventory_items_item_id",
                        column: x => x.item_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_prescription_prescription_id",
                        column: x => x.prescription_id,
                        principalTable: "prescriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_stock_lots_lot_id",
                        column: x => x.lot_id,
                        principalTable: "stock_lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_prescriptions_inventory_item_id",
                table: "prescriptions",
                column: "inventory_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_name_trgm",
                table: "inventory_items",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ux_inventory_items_name_key",
                table: "inventory_items",
                column: "name_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_item_id",
                table: "purchase_order_lines",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_purchase_order_id",
                table: "purchase_order_lines",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_branch_id_status",
                table: "purchase_orders",
                columns: new[] { "branch_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_number",
                table: "purchase_orders",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_lots_branch_id_expires_on",
                table: "stock_lots",
                columns: new[] { "branch_id", "expires_on" });

            migrationBuilder.CreateIndex(
                name: "ux_stock_lots_item_branch_lot",
                table: "stock_lots",
                columns: new[] { "item_id", "branch_id", "lot_number", "expires_on" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_branch_id",
                table: "stock_movements",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_item_id_branch_id_occurred_at",
                table: "stock_movements",
                columns: new[] { "item_id", "branch_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_lot_id",
                table: "stock_movements",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_prescription_id",
                table: "stock_movements",
                column: "prescription_id",
                filter: "prescription_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_purchase_order_id",
                table: "stock_movements",
                column: "purchase_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_prescriptions_inventory_items_inventory_item_id",
                table: "prescriptions",
                column: "inventory_item_id",
                principalTable: "inventory_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_prescriptions_inventory_items_inventory_item_id",
                table: "prescriptions");

            migrationBuilder.DropTable(
                name: "purchase_order_lines");

            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropTable(
                name: "purchase_orders");

            migrationBuilder.DropTable(
                name: "stock_lots");

            migrationBuilder.DropTable(
                name: "inventory_items");

            migrationBuilder.DropIndex(
                name: "ix_prescriptions_inventory_item_id",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "charged_unit_price_satang",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "inventory_item_id",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "stock_deducted",
                table: "prescriptions");
        }
    }
}
