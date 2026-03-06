using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NCBA.DCL.Migrations
{
    /// <inheritdoc />
    public partial class AddDeferralCloseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "Deferrals",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedById",
                table: "Deferrals",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "ClosedByName",
                table: "Deferrals",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Deferrals");

            migrationBuilder.DropColumn(
                name: "ClosedById",
                table: "Deferrals");

            migrationBuilder.DropColumn(
                name: "ClosedByName",
                table: "Deferrals");
        }
    }
}
