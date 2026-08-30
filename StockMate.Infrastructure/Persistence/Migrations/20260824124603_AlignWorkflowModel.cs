using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockMate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignWorkflowModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OrderAssignments_AssignedByStaffId",
                table: "OrderAssignments",
                column: "AssignedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAssignments_OrderId",
                table: "OrderAssignments",
                column: "OrderId",
                unique: true,
                filter: "\"ReleasedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAssignments_OrderId_ReleasedAtUtc",
                table: "OrderAssignments",
                columns: new[] { "OrderId", "ReleasedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderAssignments_StaffId",
                table: "OrderAssignments",
                column: "StaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderAssignments_Orders_OrderId",
                table: "OrderAssignments",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderAssignments_Staff_AssignedByStaffId",
                table: "OrderAssignments",
                column: "AssignedByStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderAssignments_Staff_StaffId",
                table: "OrderAssignments",
                column: "StaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderAssignments_Orders_OrderId",
                table: "OrderAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderAssignments_Staff_AssignedByStaffId",
                table: "OrderAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderAssignments_Staff_StaffId",
                table: "OrderAssignments");

            migrationBuilder.DropIndex(
                name: "IX_OrderAssignments_AssignedByStaffId",
                table: "OrderAssignments");

            migrationBuilder.DropIndex(
                name: "IX_OrderAssignments_OrderId",
                table: "OrderAssignments");

            migrationBuilder.DropIndex(
                name: "IX_OrderAssignments_OrderId_ReleasedAtUtc",
                table: "OrderAssignments");

            migrationBuilder.DropIndex(
                name: "IX_OrderAssignments_StaffId",
                table: "OrderAssignments");
        }
    }
}
