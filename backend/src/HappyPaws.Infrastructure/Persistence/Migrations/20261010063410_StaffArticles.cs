using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StaffArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "published_at",
                table: "articles",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamptz");

            migrationBuilder.AddColumn<string>(
                name: "cover_image_key",
                table: "articles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "articles",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_by_staff_id",
                table: "articles",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "first_published_at",
                table: "articles",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_cover_key",
                table: "articles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_articles_created_at_id",
                table: "articles",
                columns: new[] { "created_at", "id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_articles_deleted_not_public",
                table: "articles",
                sql: "deleted_at IS NULL OR published_at IS NULL");

            // Every article already here was published by the seed or by hand, so its first
            // publication is its publication, and its slug is fixed from now on.
            migrationBuilder.Sql("UPDATE articles SET first_published_at = published_at;");

            // content:manage is the first scope that is not in the contract's catalogue
            // (docs/contract-extensions.md § 26). clinic_admin is seeded as "every catalogue scope
            // but two", expanded once at bootstrap, so a clinic set up before this release would
            // never get it. The owner holds * and needs nothing. Other roles are the clinic's call.
            migrationBuilder.Sql(
                "UPDATE roles SET scopes = array_append(scopes, 'content:manage') " +
                "WHERE name = 'clinic_admin' AND is_system AND NOT ('content:manage' = ANY (scopes));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE roles SET scopes = array_remove(scopes, 'content:manage');");

            migrationBuilder.DropIndex(
                name: "ix_articles_created_at_id",
                table: "articles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_articles_deleted_not_public",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "cover_image_key",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "deleted_by_staff_id",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "first_published_at",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "pending_cover_key",
                table: "articles");

            // Back to a NOT NULL publication time without putting a single draft or deleted article
            // on the site: they are parked at the end of time, which the old code reads as scheduled.
            migrationBuilder.Sql("UPDATE articles SET published_at = '9999-12-31T00:00:00Z' WHERE published_at IS NULL;");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "published_at",
                table: "articles",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamptz",
                oldNullable: true);
        }
    }
}
