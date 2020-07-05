using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace M.Server.Data.Migrations
{
    public partial class initialgame : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    IsStarted = table.Column<bool>(nullable: false),
                    IsActive = table.Column<bool>(nullable: false),
                    Created = table.Column<DateTimeOffset>(nullable: false),
                    Owner = table.Column<string>(nullable: true),
                    Turn = table.Column<string>(nullable: true),
                    LastRoll1 = table.Column<int>(nullable: false),
                    LastRoll2 = table.Column<int>(nullable: false),
                    FreeParking = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Location",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Position = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    GameId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Location", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Location_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Player",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Order = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Position = table.Column<int>(nullable: false),
                    ConnectionId = table.Column<string>(nullable: true),
                    Money = table.Column<decimal>(nullable: false),
                    GameId = table.Column<Guid>(nullable: true),
                    GameId1 = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Player", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Player_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Player_Games_GameId1",
                        column: x => x.GameId1,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Location_GameId",
                table: "Location",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Player_GameId",
                table: "Player",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Player_GameId1",
                table: "Player",
                column: "GameId1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Location");

            migrationBuilder.DropTable(
                name: "Player");

            migrationBuilder.DropTable(
                name: "Games");
        }
    }
}
