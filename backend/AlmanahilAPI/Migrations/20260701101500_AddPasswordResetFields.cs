using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlmanahilAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nullable column holding the HASHED 6-digit reset code.
            migrationBuilder.AddColumn<string>(
                name: "ResetCode",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            // Nullable UTC timestamp after which the reset code is no longer valid.
            migrationBuilder.AddColumn<DateTime>(
                name: "ResetCodeExpiresAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResetCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResetCodeExpiresAt",
                table: "Users");
        }
    }
}
