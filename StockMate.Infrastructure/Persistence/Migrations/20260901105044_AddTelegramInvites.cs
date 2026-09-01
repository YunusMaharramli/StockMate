using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockMate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedByTelegramUserId = table.Column<long>(type: "bigint", nullable: true),
                    UsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramInvites", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramInvites_Token",
                table: "TelegramInvites",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramInvites");
        }
    }
}
