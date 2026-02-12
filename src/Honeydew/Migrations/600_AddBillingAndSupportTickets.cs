using System;
using Honeydew.Data;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Honeydew.Migrations
{
    [Migration("00000000000600_AddBillingAndSupportTickets")]
    public partial class AddBillingAndSupportTickets : Migration
    {
        private const string MigrationId = "00000000000600_AddBillingAndSupportTickets";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (IdempotentMigrationHelper.IsAlreadyApplied(MigrationId))
            {
                return;
            }

            migrationBuilder.CreateTable(
                name: "BillingPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    MaxUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    PricePerMonth = table.Column<decimal>(type: "TEXT", nullable: false),
                    PromotionPercent = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_BillingPlans", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_BillingPlans_Code",
                table: "BillingPlans",
                column: "Code",
                unique: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BillingPlanId",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_TenantId",
                table: "SupportTickets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_BillingPlanId",
                table: "Tenants",
                column: "BillingPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_BillingPlans_BillingPlanId",
                table: "Tenants",
                column: "BillingPlanId",
                principalTable: "BillingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_BillingPlans_BillingPlanId",
                table: "Tenants");

            migrationBuilder.DropTable(
                name: "SupportTickets");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_BillingPlanId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "BillingPlanId",
                table: "Tenants");

            migrationBuilder.DropTable(
                name: "BillingPlans");
        }
    }
}
