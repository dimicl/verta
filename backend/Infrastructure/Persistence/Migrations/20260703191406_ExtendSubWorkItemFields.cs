using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendSubWorkItemFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubWorkItemId",
                table: "work_item_files",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedUserId",
                table: "sub_work_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "sub_work_items",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SubWorkItemId",
                table: "comments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_item_files_SubWorkItemId",
                table: "work_item_files",
                column: "SubWorkItemId");

            migrationBuilder.CreateIndex(
                name: "IX_sub_work_items_AssignedUserId",
                table: "sub_work_items",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_comments_SubWorkItemId",
                table: "comments",
                column: "SubWorkItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_comments_sub_work_items_SubWorkItemId",
                table: "comments",
                column: "SubWorkItemId",
                principalTable: "sub_work_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sub_work_items_users_AssignedUserId",
                table: "sub_work_items",
                column: "AssignedUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_work_item_files_sub_work_items_SubWorkItemId",
                table: "work_item_files",
                column: "SubWorkItemId",
                principalTable: "sub_work_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_comments_sub_work_items_SubWorkItemId",
                table: "comments");

            migrationBuilder.DropForeignKey(
                name: "FK_sub_work_items_users_AssignedUserId",
                table: "sub_work_items");

            migrationBuilder.DropForeignKey(
                name: "FK_work_item_files_sub_work_items_SubWorkItemId",
                table: "work_item_files");

            migrationBuilder.DropIndex(
                name: "IX_work_item_files_SubWorkItemId",
                table: "work_item_files");

            migrationBuilder.DropIndex(
                name: "IX_sub_work_items_AssignedUserId",
                table: "sub_work_items");

            migrationBuilder.DropIndex(
                name: "IX_comments_SubWorkItemId",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "SubWorkItemId",
                table: "work_item_files");

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "sub_work_items");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "sub_work_items");

            migrationBuilder.DropColumn(
                name: "SubWorkItemId",
                table: "comments");
        }
    }
}
