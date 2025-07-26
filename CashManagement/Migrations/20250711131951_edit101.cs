using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashManagement.Migrations
{
    /// <inheritdoc />
    public partial class edit101 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MonthlyUsed",
                table: "CashLines",
                newName: "MonthlyWithdrawUsed");

            migrationBuilder.RenameColumn(
                name: "MonthlyLimit",
                table: "CashLines",
                newName: "MonthlyWithdrawLimit");

            migrationBuilder.RenameColumn(
                name: "DailyUsed",
                table: "CashLines",
                newName: "MonthlyDepositUsed");

            migrationBuilder.RenameColumn(
                name: "DailyLimit",
                table: "CashLines",
                newName: "MonthlyDepositLimit");

            migrationBuilder.AddColumn<decimal>(
                name: "DailyDepositLimit",
                table: "CashLines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyDepositUsed",
                table: "CashLines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyWithdrawLimit",
                table: "CashLines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyWithdrawUsed",
                table: "CashLines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "CashLines",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyDepositLimit",
                table: "CashLines");

            migrationBuilder.DropColumn(
                name: "DailyDepositUsed",
                table: "CashLines");

            migrationBuilder.DropColumn(
                name: "DailyWithdrawLimit",
                table: "CashLines");

            migrationBuilder.DropColumn(
                name: "DailyWithdrawUsed",
                table: "CashLines");

            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "CashLines");

            migrationBuilder.RenameColumn(
                name: "MonthlyWithdrawUsed",
                table: "CashLines",
                newName: "MonthlyUsed");

            migrationBuilder.RenameColumn(
                name: "MonthlyWithdrawLimit",
                table: "CashLines",
                newName: "MonthlyLimit");

            migrationBuilder.RenameColumn(
                name: "MonthlyDepositUsed",
                table: "CashLines",
                newName: "DailyUsed");

            migrationBuilder.RenameColumn(
                name: "MonthlyDepositLimit",
                table: "CashLines",
                newName: "DailyLimit");
        }
    }
}
