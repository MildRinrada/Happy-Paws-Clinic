using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerMessageThreads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "message_threads",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    last_message_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_inbound_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_inbound_channel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    last_reply_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_threads", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_threads_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "thread_messages",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    thread_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    channel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    text = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sent_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    delivery_status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    delivery_error_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    fallback_from = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_thread_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_thread_messages_message_threads_thread_id",
                        column: x => x.thread_id,
                        principalTable: "message_threads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_message_threads_branch_id",
                table: "message_threads",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_threads_customer_id",
                table: "message_threads",
                column: "customer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_message_threads_last_message_at",
                table: "message_threads",
                column: "last_message_at");

            migrationBuilder.CreateIndex(
                name: "ix_thread_messages_channel_external_id",
                table: "thread_messages",
                columns: new[] { "channel", "external_id" },
                unique: true,
                filter: "external_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_thread_messages_thread_id_sent_at",
                table: "thread_messages",
                columns: new[] { "thread_id", "sent_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "thread_messages");

            migrationBuilder.DropTable(
                name: "message_threads");
        }
    }
}
