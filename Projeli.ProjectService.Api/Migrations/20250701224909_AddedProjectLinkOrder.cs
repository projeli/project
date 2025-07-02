using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projeli.ProjectService.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddedProjectLinkOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "ProjectLinks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "ProjectLinks");
        }
    }
}
