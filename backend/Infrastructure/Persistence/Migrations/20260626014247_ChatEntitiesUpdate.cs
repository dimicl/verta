using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChatEntitiesUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EditedAt",
                table: "messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEdited",
                table: "messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "conversations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "conversations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "conversations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "conversations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "JoinedAt",
                table: "conversation_participants",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReadAt",
                table: "conversation_participants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastReadMessageId",
                table: "conversation_participants",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_conversations_CreatedByUserId",
                table: "conversations",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_conversations_users_CreatedByUserId",
                table: "conversations",
                column: "CreatedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_conversations_users_CreatedByUserId",
                table: "conversations");

            migrationBuilder.DropIndex(
                name: "IX_conversations_CreatedByUserId",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "EditedAt",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "IsEdited",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "LastReadAt",
                table: "conversation_participants");

            migrationBuilder.DropColumn(
                name: "LastReadMessageId",
                table: "conversation_participants");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "messages",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "conversations",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "JoinedAt",
                table: "conversation_participants",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }
    }
}
