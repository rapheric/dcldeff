using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NCBA.DCL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoanAmount",
                table: "Deferrals");

            migrationBuilder.AddColumn<int>(
                name: "DaysSought",
                table: "DeferralDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextDocumentDueDate",
                table: "DeferralDocuments",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DaysSought",
                table: "DeferralDocuments");

            migrationBuilder.DropColumn(
                name: "NextDocumentDueDate",
                table: "DeferralDocuments");

            migrationBuilder.AddColumn<decimal>(
                name: "LoanAmount",
                table: "Deferrals",
                type: "decimal(65,30)",
                nullable: true);
        }
    }
}
