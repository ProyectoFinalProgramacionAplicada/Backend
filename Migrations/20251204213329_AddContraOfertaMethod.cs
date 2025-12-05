using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruekAppAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddContraOfertaMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastOfferByUserId",
                table: "Trades",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastOfferByUserId",
                table: "Trades");
        }
    }
}
