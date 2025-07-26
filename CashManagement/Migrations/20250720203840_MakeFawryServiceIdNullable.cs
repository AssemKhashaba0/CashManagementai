using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashManagement.Migrations
{
    /// <inheritdoc />
    public partial class MakeFawryServiceIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FawryTransactions_FawryServices_FawryServiceId",
                table: "FawryTransactions");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceId",
                table: "FawryTransactions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "FawryServiceId",
                table: "FawryTransactions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_FawryTransactions_FawryServices_FawryServiceId",
                table: "FawryTransactions",
                column: "FawryServiceId",
                principalTable: "FawryServices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FawryTransactions_FawryServices_FawryServiceId",
                table: "FawryTransactions");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceId",
                table: "FawryTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FawryServiceId",
                table: "FawryTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FawryTransactions_FawryServices_FawryServiceId",
                table: "FawryTransactions",
                column: "FawryServiceId",
                principalTable: "FawryServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
