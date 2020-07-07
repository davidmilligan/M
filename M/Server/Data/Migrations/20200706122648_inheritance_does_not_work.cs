using Microsoft.EntityFrameworkCore.Migrations;

namespace M.Server.Data.Migrations
{
    public partial class inheritance_does_not_work : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "Location",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Improvements",
                table: "Location",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsMortgaged",
                table: "Location",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Owner",
                table: "Location",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Location",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Group",
                table: "Location");

            migrationBuilder.DropColumn(
                name: "Improvements",
                table: "Location");

            migrationBuilder.DropColumn(
                name: "IsMortgaged",
                table: "Location");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "Location");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Location");
        }
    }
}
