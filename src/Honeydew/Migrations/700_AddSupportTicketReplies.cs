using System;
using Honeydew.Data;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Honeydew.Migrations
{
    [Migration("00000000000700_AddSupportTicketReplies")]
    public partial class AddSupportTicketReplies : Migration
    {
        private const string MigrationId = "00000000000700_AddSupportTicketReplies";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (IdempotentMigrationHelper.IsAlreadyApplied(MigrationId))
            {
                return;
            }

            migrationBuilder.CreateTable(
                name: "SupportTicketReplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupportTicketId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    IsFromStaff = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketReplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTicketReplies_SupportTickets_SupportTicketId",
                        column: x => x.SupportTicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketReplies_SupportTicketId",
                table: "SupportTicketReplies",
                column: "SupportTicketId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SupportTicketReplies");
        }
    }
}
