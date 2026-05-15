using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserStatusAndUniqueEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE users
                ALTER COLUMN "UpdatedAt"
                TYPE timestamp with time zone
                USING NULL;
            """);

            migrationBuilder.Sql("""
                ALTER TABLE users
                ALTER COLUMN "CreatedAt"
                TYPE timestamp with time zone
                USING CURRENT_TIMESTAMP;
            """);

            migrationBuilder.Sql("""
                ALTER TABLE users
                ALTER COLUMN "Status"
                TYPE integer
                USING CASE
                    WHEN "Status" = 'active' THEN 0
                    WHEN "Status" = 'Active' THEN 0
                    WHEN "Status" = 'inactive' THEN 1
                    WHEN "Status" = 'Inactive' THEN 1
                    WHEN "Status" = 'blocked' THEN 2
                    WHEN "Status" = 'Blocked' THEN 2
                    ELSE 0
                END;
            """);

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_Email",
                table: "users");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedAt",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedAt",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }
    }
}
