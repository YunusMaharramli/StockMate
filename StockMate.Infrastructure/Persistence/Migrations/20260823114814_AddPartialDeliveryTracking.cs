using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockMate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPartialDeliveryTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CancelledQuantity",
                table: "OrderItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeliveredQuantity",
                table: "OrderItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE "OrderItems" AS oi
                SET "DeliveredQuantity" =
                    oi."FromStockQuantity" + oi."SpecialOrderQuantity"
                FROM "Orders" AS o
                WHERE oi."OrderId" = o."Id"
                  AND o."Status" = 5;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelledQuantity",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "DeliveredQuantity",
                table: "OrderItems");
        }
    }
}
