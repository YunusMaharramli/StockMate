using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockMate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    OperationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResultJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedOperations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedOperations_CreatedAtUtc",
                table: "ProcessedOperations",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedOperations_TelegramUserId_OperationId",
                table: "ProcessedOperations",
                columns: new[] { "TelegramUserId", "OperationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedOperations");
        }
    }
}
