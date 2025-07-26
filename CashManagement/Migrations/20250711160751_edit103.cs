using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CashManagement.Migrations
{
    /// <inheritdoc />
    public partial class edit103 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "FawryServices",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "FawryServices",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "FawryServices",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "FawryServices",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "FawryServices",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "FawryServices",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
