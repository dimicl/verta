using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixDomainEventLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DomainEventLogs",
                table: "DomainEventLogs");

            migrationBuilder.RenameTable(
                name: "DomainEventLogs",
                newName: "domain_event_logs");

            migrationBuilder.AlterColumn<string>(
                name: "QueueName",
                table: "domain_event_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "EventName",
                table: "domain_event_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_domain_event_logs",
                table: "domain_event_logs",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_domain_event_logs_EventName",
                table: "domain_event_logs",
                column: "EventName");

            migrationBuilder.CreateIndex(
                name: "IX_domain_event_logs_ReceivedAt",
                table: "domain_event_logs",
                column: "ReceivedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_domain_event_logs",
                table: "domain_event_logs");

            migrationBuilder.DropIndex(
                name: "IX_domain_event_logs_EventName",
                table: "domain_event_logs");

            migrationBuilder.DropIndex(
                name: "IX_domain_event_logs_ReceivedAt",
                table: "domain_event_logs");

            migrationBuilder.RenameTable(
                name: "domain_event_logs",
                newName: "DomainEventLogs");

            migrationBuilder.AlterColumn<string>(
                name: "QueueName",
                table: "DomainEventLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "EventName",
                table: "DomainEventLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DomainEventLogs",
                table: "DomainEventLogs",
                column: "Id");
        }
    }
}
