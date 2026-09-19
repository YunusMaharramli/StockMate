using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockMate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderConcurrencyTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "OrderShipments",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Orders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "OrderShipments");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Orders");
        }
    }
}
