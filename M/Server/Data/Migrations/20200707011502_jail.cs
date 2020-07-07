using Microsoft.EntityFrameworkCore.Migrations;

namespace M.Server.Data.Migrations
{
    public partial class jail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupColor",
                table: "Location");

            migrationBuilder.AddColumn<bool>(
                name: "IsInJail",
                table: "Player",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Location",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInJail",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Location");

            migrationBuilder.AddColumn<string>(
                name: "GroupColor",
                table: "Location",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
