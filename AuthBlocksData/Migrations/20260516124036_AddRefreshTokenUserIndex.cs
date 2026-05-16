using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthBlocksData.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenUserIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_UserId",
                schema: "auth",
                table: "refresh_tokens");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId_ExpiresAt",
                schema: "auth",
                table: "refresh_tokens",
                columns: new[] { "UserId", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_UserId_ExpiresAt",
                schema: "auth",
                table: "refresh_tokens");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                schema: "auth",
                table: "refresh_tokens",
                column: "UserId");
        }
    }
}
