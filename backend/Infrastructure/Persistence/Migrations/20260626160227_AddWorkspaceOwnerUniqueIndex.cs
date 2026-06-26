using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceOwnerUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workspaces_OwnerId",
                table: "workspaces");

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_OwnerId",
                table: "workspaces",
                column: "OwnerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workspaces_OwnerId",
                table: "workspaces");

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_OwnerId",
                table: "workspaces",
                column: "OwnerId");
        }
    }
}
