using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlmanahilAPI.Migrations
{
    /// <inheritdoc />
    public partial class ClassStructureUpgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcademicYear",
                table: "Classes",
                type: "character varying(9)",
                maxLength: 9,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Grade",
                table: "Classes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "Classes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "Classes",
                type: "character varying(1)",
                maxLength: 1,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "Section",
                table: "Classes");
        }
    }
}
