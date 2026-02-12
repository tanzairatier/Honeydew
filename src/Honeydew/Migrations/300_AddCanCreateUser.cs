using Honeydew.Data;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Honeydew.Migrations
{
    [Migration("00000000000300_AddCanCreateUser")]
    public partial class AddCanCreateUser : Migration
    {
        private const string MigrationId = "00000000000300_AddCanCreateUser";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (IdempotentMigrationHelper.IsAlreadyApplied(MigrationId))
            {
                return;
            }

            migrationBuilder.AddColumn<bool>(
                name: "CanCreateUser",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanCreateUser",
                table: "Users");
        }
    }
}
