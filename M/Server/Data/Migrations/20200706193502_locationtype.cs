using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace M.Server.Data.Migrations
{
    public partial class locationtype : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroupColor",
                table: "Location",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Rate",
                table: "Location",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tax",
                table: "Location",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TurnMessage",
                table: "Location",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Location",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Message",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(nullable: false),
                    From = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true),
                    GameId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Message", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Message_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Message_GameId",
                table: "Message",
                column: "GameId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Message");

            migrationBuilder.DropColumn(
                name: "GroupColor",
                table: "Location");

            migrationBuilder.DropColumn(
                name: "Rate",
                table: "Location");

            migrationBuilder.DropColumn(
                name: "Tax",
                table: "Location");

            migrationBuilder.DropColumn(
                name: "TurnMessage",
                table: "Location");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Location");
        }
    }
}
