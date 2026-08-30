using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockMate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SaleRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<long>(type: "bigint", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryCompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SoldAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleRecords_DeliveryCompanies_DeliveryCompanyId",
                        column: x => x.DeliveryCompanyId,
                        principalTable: "DeliveryCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleRecords_OrderShipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "OrderShipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleRecords_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleRecordItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SaleRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleRecordItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleRecordItems_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleRecordItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleRecordItems_SaleRecords_SaleRecordId",
                        column: x => x.SaleRecordId,
                        principalTable: "SaleRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaleRecordItems_OrderItemId",
                table: "SaleRecordItems",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleRecordItems_ProductId",
                table: "SaleRecordItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleRecordItems_SaleRecordId",
                table: "SaleRecordItems",
                column: "SaleRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleRecords_DeliveryCompanyId_SoldAtUtc",
                table: "SaleRecords",
                columns: new[] { "DeliveryCompanyId", "SoldAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleRecords_OrderId",
                table: "SaleRecords",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleRecords_OrderNumber",
                table: "SaleRecords",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SaleRecords_ShipmentId",
                table: "SaleRecords",
                column: "ShipmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleRecords_SoldAtUtc",
                table: "SaleRecords",
                column: "SoldAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaleRecordItems");

            migrationBuilder.DropTable(
                name: "SaleRecords");
        }
    }
}
