using Microsoft.EntityFrameworkCore.Migrations;

namespace SlackCoffee.Migrations
{
    public partial class AddSteamInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SteamMilkNeeded",
                table: "menus",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SteamMilkNeeded",
                table: "menus");
        }
    }
}
