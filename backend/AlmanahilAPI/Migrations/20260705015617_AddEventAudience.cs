using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlmanahilAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEventAudience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudienceType",
                table: "Events",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "AllUsers");

            migrationBuilder.AddColumn<int>(
                name: "TargetClassId",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_TargetClassId",
                table: "Events",
                column: "TargetClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Classes_TargetClassId",
                table: "Events",
                column: "TargetClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Classes_TargetClassId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_TargetClassId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "AudienceType",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "TargetClassId",
                table: "Events");
        }
    }
}
