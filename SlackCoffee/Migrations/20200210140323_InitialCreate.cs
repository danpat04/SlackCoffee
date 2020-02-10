using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SlackCoffee.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompletedOrders",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    UserId = table.Column<string>(maxLength: 15, nullable: true),
                    MenuId = table.Column<string>(maxLength: 40, nullable: true),
                    Options = table.Column<string>(nullable: true),
                    OrderedAt = table.Column<DateTime>(nullable: false),
                    ShotCount = table.Column<int>(nullable: false),
                    Price = table.Column<int>(nullable: false),
                    PickedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletedOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "menus",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 10, nullable: false),
                    Description = table.Column<string>(nullable: true),
                    Price = table.Column<int>(nullable: false),
                    Order = table.Column<int>(nullable: false),
                    Enabled = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    UserId = table.Column<string>(maxLength: 15, nullable: true),
                    MenuId = table.Column<string>(maxLength: 40, nullable: true),
                    Options = table.Column<string>(nullable: true),
                    OrderedAt = table.Column<DateTime>(nullable: false),
                    ShotCount = table.Column<int>(nullable: false),
                    Price = table.Column<int>(nullable: false),
                    PickedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Deposit = table.Column<int>(nullable: false),
                    IsManager = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WalletHistory",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    UserId = table.Column<string>(nullable: true),
                    Amount = table.Column<int>(nullable: false),
                    At = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletHistory_UserId",
                table: "WalletHistory",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompletedOrders");

            migrationBuilder.DropTable(
                name: "menus");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "WalletHistory");
        }
    }
}
