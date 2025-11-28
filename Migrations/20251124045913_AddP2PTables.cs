using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruekAppAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddP2PTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "P2POrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CreatorUserId = table.Column<int>(type: "int", nullable: false),
                    CounterpartyUserId = table.Column<int>(type: "int", nullable: true),
                    AmountBob = table.Column<double>(type: "float", nullable: false),
                    AmountTrueCoins = table.Column<double>(type: "float", nullable: false),
                    Rate = table.Column<double>(type: "float", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P2POrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_P2POrders_Users_CounterpartyUserId",
                        column: x => x.CounterpartyUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_P2POrders_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_P2POrders_CounterpartyUserId",
                table: "P2POrders",
                column: "CounterpartyUserId");

            migrationBuilder.CreateIndex(
                name: "IX_P2POrders_CreatorUserId",
                table: "P2POrders",
                column: "CreatorUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "P2POrders");
        }
    }
}
