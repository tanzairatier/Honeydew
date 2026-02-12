using System;
using Honeydew.Data;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Honeydew.Migrations
{
    [Migration("00000000000500_AddTodoAssignedDueDateAndVotes")]
    public partial class AddTodoAssignedDueDateAndVotes : Migration
    {
        private const string MigrationId = "00000000000500_AddTodoAssignedDueDateAndVotes";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (IdempotentMigrationHelper.IsAlreadyApplied(MigrationId))
            {
                return;
            }

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToUserId",
                table: "TodoItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "TodoItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("UPDATE TodoItems SET AssignedToUserId = CreatedByUserId WHERE AssignedToUserId IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_AssignedToUserId",
                table: "TodoItems",
                column: "AssignedToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TodoItems_Users_AssignedToUserId",
                table: "TodoItems",
                column: "AssignedToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.CreateTable(
                name: "TodoItemVotes",
                columns: table => new
                {
                    TodoItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItemVotes", x => new { x.TodoItemId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TodoItemVotes_TodoItems_TodoItemId",
                        column: x => x.TodoItemId,
                        principalTable: "TodoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TodoItemVotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItemVotes_TodoItemId",
                table: "TodoItemVotes",
                column: "TodoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItemVotes_UserId",
                table: "TodoItemVotes",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TodoItems_Users_AssignedToUserId",
                table: "TodoItems");

            migrationBuilder.DropTable(
                name: "TodoItemVotes");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_AssignedToUserId",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "TodoItems");
        }
    }
}
