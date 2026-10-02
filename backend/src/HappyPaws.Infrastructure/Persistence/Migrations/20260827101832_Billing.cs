using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Billing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    consultation_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    issued_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    voided_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    voided_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    void_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoices_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invoices_consultation_consultation_id",
                        column: x => x.consultation_id,
                        principalTable: "consultations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invoices_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invoices_pet_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_discounts",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    invoice_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    amount_satang = table.Column<long>(type: "bigint", nullable: false),
                    percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    approved_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    applied_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    applied_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_discounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_discounts_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_lines",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    invoice_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price_satang = table.Column<long>(type: "bigint", nullable: false),
                    source_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_lines_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_payments",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    invoice_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount_satang = table.Column<long>(type: "bigint", nullable: false),
                    reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    collected_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    collected_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_payments_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_refunds",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    invoice_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    payment_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount_satang = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    refunded_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    refunded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_refunds", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_refunds_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "receipts",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    invoice_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_tax_invoice = table.Column<bool>(type: "boolean", nullable: false),
                    amount_satang = table.Column<long>(type: "bigint", nullable: false),
                    tax_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    billing_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    issued_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receipts", x => x.id);
                    table.ForeignKey(
                        name: "fk_receipts_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_discounts_approved_by_staff_id_applied_at",
                table: "invoice_discounts",
                columns: new[] { "approved_by_staff_id", "applied_at" });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_discounts_invoice_id",
                table: "invoice_discounts",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_invoice_id",
                table: "invoice_lines",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_kind",
                table: "invoice_lines",
                column: "kind");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_payments_collected_at_method",
                table: "invoice_payments",
                columns: new[] { "collected_at", "method" });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_payments_invoice_id",
                table: "invoice_payments",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_refunds_invoice_id",
                table: "invoice_refunds",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_refunds_refunded_at",
                table: "invoice_refunds",
                column: "refunded_at");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_branch_id_status_issued_at",
                table: "invoices",
                columns: new[] { "branch_id", "status", "issued_at" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_consultation_id",
                table: "invoices",
                column: "consultation_id",
                unique: true,
                filter: "consultation_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_customer_id_issued_at",
                table: "invoices",
                columns: new[] { "customer_id", "issued_at" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_number",
                table: "invoices",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_pet_id",
                table: "invoices",
                column: "pet_id");

            migrationBuilder.CreateIndex(
                name: "ix_receipts_invoice_id",
                table: "receipts",
                column: "invoice_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_receipts_number",
                table: "receipts",
                column: "number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_discounts");

            migrationBuilder.DropTable(
                name: "invoice_lines");

            migrationBuilder.DropTable(
                name: "invoice_payments");

            migrationBuilder.DropTable(
                name: "invoice_refunds");

            migrationBuilder.DropTable(
                name: "receipts");

            migrationBuilder.DropTable(
                name: "invoices");
        }
    }
}
