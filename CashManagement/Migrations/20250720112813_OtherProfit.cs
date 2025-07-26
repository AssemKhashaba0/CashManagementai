using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CashManagement.Migrations
{
    /// <inheritdoc />
    public partial class OtherProfit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FawryTransactions");

            migrationBuilder.DropTable(
                name: "FawryServices");

            migrationBuilder.CreateTable(
                name: "OtherProfits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DepositType = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtherProfits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtherProfits_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OtherProfits_UserId",
                table: "OtherProfits",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OtherProfits");

            migrationBuilder.CreateTable(
                name: "FawryServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FeesPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsManualFees = table.Column<bool>(type: "bit", nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServiceType = table.Column<int>(type: "int", nullable: false),
                    SubServiceType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FawryServices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FawryTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FawryServiceId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FeesAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubServiceType = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FawryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FawryTransactions_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FawryTransactions_FawryServices_FawryServiceId",
                        column: x => x.FawryServiceId,
                        principalTable: "FawryServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "FawryServices",
                columns: new[] { "Id", "CreatedAt", "Description", "FeesPercentage", "IsActive", "IsManualFees", "ServiceName", "ServiceType", "SubServiceType" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 7, 11, 16, 7, 48, 960, DateTimeKind.Utc).AddTicks(207), "خدمة سحب نقدي عبر فوري", 10m, true, false, "سحب كاش", 0, 0 },
                    { 2, new DateTime(2025, 7, 11, 16, 7, 48, 960, DateTimeKind.Utc).AddTicks(214), "خدمة سحب عبر فيزا مشتريات مع أرباح يدوية", null, true, true, "سحب فيزا مشتريات", 0, 1 },
                    { 3, new DateTime(2025, 7, 11, 16, 7, 48, 960, DateTimeKind.Utc).AddTicks(218), "خدمة سحب عبر فيزا عادي مع أرباح يدوية", null, true, true, "سحب فيزا", 0, 2 },
                    { 4, new DateTime(2025, 7, 11, 16, 7, 48, 960, DateTimeKind.Utc).AddTicks(222), "خدمة إيداع رئيسي دون أرباح", null, true, false, "إيداع رئيسي", 1, 3 },
                    { 5, new DateTime(2025, 7, 11, 16, 7, 48, 960, DateTimeKind.Utc).AddTicks(226), "خدمة إيداع تسييل دون أرباح", null, true, false, "إيداع تسييل", 1, 4 },
                    { 6, new DateTime(2025, 7, 11, 16, 7, 48, 960, DateTimeKind.Utc).AddTicks(230), "خدمة إيداع توريد دون أرباح", null, true, false, "إيداع توريد", 1, 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FawryTransactions_ApplicationUserId",
                table: "FawryTransactions",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FawryTransactions_FawryServiceId",
                table: "FawryTransactions",
                column: "FawryServiceId");
        }
    }
}
