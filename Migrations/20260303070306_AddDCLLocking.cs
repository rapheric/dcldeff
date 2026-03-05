using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NCBA.DCL.Migrations
{
    /// <inheritdoc />
    public partial class AddDCLLocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "Checklists",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LockedByUserId",
                table: "Checklists",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_LockedByUserId",
                table: "Checklists",
                column: "LockedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Checklists_Users_LockedByUserId",
                table: "Checklists",
                column: "LockedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Checklists_Users_LockedByUserId",
                table: "Checklists");

            migrationBuilder.DropIndex(
                name: "IX_Checklists_LockedByUserId",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "LockedByUserId",
                table: "Checklists");
        }
    }
}
