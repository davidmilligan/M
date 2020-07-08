using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace M.Server.Data.Migrations
{
    public partial class randomevents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GetOutOfJailFree",
                table: "Player",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Location",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DoubleCount",
                table: "Games",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RandomEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    Amount = table.Column<decimal>(nullable: false),
                    PayTarget = table.Column<int>(nullable: false),
                    MoveTarget = table.Column<int>(nullable: false),
                    MoveAmount = table.Column<int>(nullable: false),
                    RentAdjustment = table.Column<decimal>(nullable: false),
                    SpecialEvent = table.Column<int>(nullable: false),
                    LocationId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RandomEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RandomEvent_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RandomEvent_LocationId",
                table: "RandomEvent",
                column: "LocationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RandomEvent");

            migrationBuilder.DropColumn(
                name: "GetOutOfJailFree",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Location");

            migrationBuilder.DropColumn(
                name: "DoubleCount",
                table: "Games");
        }
    }
}
