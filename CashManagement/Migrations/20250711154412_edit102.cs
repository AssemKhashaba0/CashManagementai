using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashManagement.Migrations
{
    /// <inheritdoc />
    public partial class edit102 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "FeesAmount",
                table: "FawryTransactions",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<int>(
                name: "SubServiceType",
                table: "FawryTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsManualFees",
                table: "FawryServices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ServiceType",
                table: "FawryServices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubServiceType",
                table: "FawryServices",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubServiceType",
                table: "FawryTransactions");

            migrationBuilder.DropColumn(
                name: "IsManualFees",
                table: "FawryServices");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "FawryServices");

            migrationBuilder.DropColumn(
                name: "SubServiceType",
                table: "FawryServices");

            migrationBuilder.AlterColumn<decimal>(
                name: "FeesAmount",
                table: "FawryTransactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }
    }
}
