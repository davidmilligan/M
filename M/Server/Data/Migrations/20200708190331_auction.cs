using Microsoft.EntityFrameworkCore.Migrations;

namespace M.Server.Data.Migrations
{
    public partial class auction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "RandomEvent",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoveTargetGroup",
                table: "RandomEvent",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentBid",
                table: "Player",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "HasBid",
                table: "Player",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MoneyOwed",
                table: "Player",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MoneyOwedTo",
                table: "Player",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ForSaleAmount",
                table: "Location",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ForSaleTo",
                table: "Location",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RentOverride",
                table: "Location",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "AuctionProperty",
                table: "Games",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AuctionsEnabled",
                table: "Games",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RentAdjustment",
                table: "Games",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DefaultIcon",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GamesLost",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GamesWon",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalMoneyWon",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Icon",
                table: "RandomEvent");

            migrationBuilder.DropColumn(
                name: "MoveTargetGroup",
                table: "RandomEvent");

            migrationBuilder.DropColumn(
                name: "CurrentBid",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "HasBid",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "MoneyOwed",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "MoneyOwedTo",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "ForSaleAmount",
                table: "Location");

            migrationBuilder.DropColumn(
                name: "ForSaleTo",
                table: "Location");

            migrationBuilder.DropColumn(
                name: "RentOverride",
                table: "Location");

            migrationBuilder.DropColumn(
                name: "AuctionProperty",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "AuctionsEnabled",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "RentAdjustment",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "DefaultIcon",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GamesLost",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GamesWon",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TotalMoneyWon",
                table: "AspNetUsers");
        }
    }
}
