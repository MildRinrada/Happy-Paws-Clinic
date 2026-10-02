using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Articles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "articles",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    body = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    author_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    reading_minutes = table.Column<int>(type: "integer", nullable: false),
                    cover_image_path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_articles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_articles_published_at",
                table: "articles",
                column: "published_at");

            migrationBuilder.CreateIndex(
                name: "ix_articles_slug",
                table: "articles",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "articles");
        }
    }
}
