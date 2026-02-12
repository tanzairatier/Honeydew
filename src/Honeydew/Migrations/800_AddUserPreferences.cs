using Honeydew.Data;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Honeydew.Migrations
{
    [Migration("00000000000800_AddUserPreferences")]
    public partial class AddUserPreferences : Migration
    {
        private const string MigrationId = "00000000000800_AddUserPreferences";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (IdempotentMigrationHelper.IsAlreadyApplied(MigrationId))
            {
                return;
            }

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemsPerPage = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UserPreferences");
        }
    }
}
