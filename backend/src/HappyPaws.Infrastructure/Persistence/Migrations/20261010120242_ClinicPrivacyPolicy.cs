using System;
using System.Text.Json;
using HappyPaws.Domain.PrivacyPolicies;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClinicPrivacyPolicy : Migration
    {
        /// <summary>The draft seeded from the shipped template.</summary>
        public const string SeededDraftId = "ppv_seed_template";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "privacy_policy_versions",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    sections = table.Column<string>(type: "jsonb", nullable: false),
                    created_from = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    template_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    based_on_version_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    created_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    label = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    published_by_staff_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    legal_review_done = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_privacy_policy_versions", x => x.id);
                    table.CheckConstraint("ck_privacy_policy_versions_published_complete", "(published_at IS NULL AND label IS NULL AND effective_date IS NULL) OR (published_at IS NOT NULL AND label IS NOT NULL AND effective_date IS NOT NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "ix_privacy_policy_versions_label",
                table: "privacy_policy_versions",
                column: "label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_privacy_policy_versions_published_at",
                table: "privacy_policy_versions",
                column: "published_at");

            // The template that ships in the code, stored as a DRAFT only (docs/contract-extensions.md
            // § 35). Nothing is published here: the clinic is the data controller, and until its owner
            // publishes a version the public page says no notice has been published yet. The text is
            // the template as the code has it when this migration runs, serialised the way
            // JsonListColumn writes the sections column.
            migrationBuilder.InsertData(
                table: "privacy_policy_versions",
                columns: ["id", "sections", "created_from", "template_version", "legal_review_done", "created_at"],
                columnTypes: ["character varying(40)", "jsonb", "character varying(16)", "character varying(40)", "boolean", "timestamptz"],
                values: new object[]
                {
                    SeededDraftId,
                    JsonSerializer.Serialize(PrivacyPolicyTemplate.Sections, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    nameof(PrivacyPolicySource.Template),
                    PrivacyPolicyTemplate.Version,
                    false,
                    DateTimeOffset.UtcNow,
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "privacy_policy_versions");
        }
    }
}
