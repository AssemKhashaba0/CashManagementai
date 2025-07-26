using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddManualFeesToFawryTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FawryTransactions");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "FawryTransactions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "FawryTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "FawryTransactions",
                type: "datetime2",
                nullable: true);
        }
    }
}
