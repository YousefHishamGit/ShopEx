using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopEx.Infrastructure.Persistence.ShopDb.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BalanceId",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeviceId",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BalanceId",
                table: "Expenses",
                column: "BalanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_DeviceId",
                table: "Expenses",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Expenses_BalanceId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_DeviceId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "BalanceId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "Expenses");
        }
    }
}
