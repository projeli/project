using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projeli.ProjectService.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddedProjectStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Projects");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Projects");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Projects",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
