using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PetIdentityAndUndoDeath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "birth_date",
                table: "pets",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "birth_date_estimated",
                table: "pets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deceased_undone_at",
                table: "pets",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "neutered",
                table: "pets",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sex",
                table: "pets",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                // Every patient registered before this column existed was never sexed in the system.
                defaultValue: "Unknown");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "birth_date",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "birth_date_estimated",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "deceased_undone_at",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "neutered",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "sex",
                table: "pets");
        }
    }
}
