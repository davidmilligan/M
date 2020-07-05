using Microsoft.EntityFrameworkCore.Migrations;

namespace M.Server.Data.Migrations
{
    public partial class playericon : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Player",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Player");
        }
    }
}
