using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_reviews",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    visit_on = table.Column<DateOnly>(type: "date", nullable: false),
                    visit_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    pet_species = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    name_choice = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    public_name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    invitation_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    moderated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    moderated_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    reject_reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    reply_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    replied_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    replied_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_reviews", x => x.id);
                    table.CheckConstraint("ck_customer_reviews_published_has_time", "status <> 'Published' OR published_at IS NOT NULL");
                    table.CheckConstraint("ck_customer_reviews_rating", "rating BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_customer_reviews_rejected_has_reason", "(status = 'Rejected') = (reject_reason IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_customer_reviews_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_customer_reviews_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "review_invitations",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    visit_on = table.Column<DateOnly>(type: "date", nullable: false),
                    visit_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pet_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    service_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    dispatch_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    used_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    review_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_invitations", x => x.id);
                    table.ForeignKey(
                        name: "fk_review_invitations_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_review_invitations_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_reviews_branch_id",
                table: "customer_reviews",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_reviews_status_created_at_id",
                table: "customer_reviews",
                columns: new[] { "status", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_reviews_visit",
                table: "customer_reviews",
                columns: new[] { "customer_id", "branch_id", "visit_on" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_review_invitations_branch_id",
                table: "review_invitations",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_invitations_token_hash",
                table: "review_invitations",
                column: "token_hash",
                unique: true,
                filter: "token_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_review_invitations_visit",
                table: "review_invitations",
                columns: new[] { "customer_id", "branch_id", "visit_on" },
                unique: true);

            // The starter review invitation (docs/contract-extensions.md § 36): two days after the
            // visit at 09:00, the approved default. Seeded switched OFF like the other starter
            // rules: it messages customers, under marketing consent, only once the clinic has
            // previewed it with /simulate and opted in.
            migrationBuilder.Sql(
                """
                INSERT INTO automation_rules
                    (id, name, trigger, offset_days, offset_hours, send_time, unconfirmed_only,
                     channel, template_key, enabled, created_at)
                VALUES
                    ('atm_starter_review_2d', 'ชวนรีวิวหลังมาใช้บริการ 2 วัน', 'VisitCompleted', -2, NULL, '09:00', false,
                     'Auto', 'review.invitation', false, now())
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Older code cannot read the VisitCompleted trigger, so every rule on it goes, with what
            // it sent.
            migrationBuilder.Sql(
                """
                DELETE FROM automation_dispatches
                WHERE rule_id IN (SELECT id FROM automation_rules WHERE trigger = 'VisitCompleted');
                DELETE FROM automation_rules WHERE trigger = 'VisitCompleted';
                """);

            migrationBuilder.DropTable(
                name: "customer_reviews");

            migrationBuilder.DropTable(
                name: "review_invitations");
        }
    }
}
