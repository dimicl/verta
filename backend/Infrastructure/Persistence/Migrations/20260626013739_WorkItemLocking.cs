using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkItemLocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "work_item_lock_interests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkItemId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_item_lock_interests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_item_lock_interests_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_work_item_lock_interests_work_items_WorkItemId",
                        column: x => x.WorkItemId,
                        principalTable: "work_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_item_locks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkItemId = table.Column<int>(type: "integer", nullable: false),
                    LockedByUserId = table.Column<int>(type: "integer", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_item_locks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_item_locks_users_LockedByUserId",
                        column: x => x.LockedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_item_locks_work_items_WorkItemId",
                        column: x => x.WorkItemId,
                        principalTable: "work_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_work_item_lock_interests_UserId",
                table: "work_item_lock_interests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_work_item_lock_interests_WorkItemId_UserId",
                table: "work_item_lock_interests",
                columns: new[] { "WorkItemId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_item_locks_LockedByUserId",
                table: "work_item_locks",
                column: "LockedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_work_item_locks_WorkItemId",
                table: "work_item_locks",
                column: "WorkItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_item_lock_interests");

            migrationBuilder.DropTable(
                name: "work_item_locks");
        }
    }
}
