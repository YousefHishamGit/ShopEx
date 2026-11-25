using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopEx.Infrastructure.Persistence.ShopDb.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardDateIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_DeliveredAt",
                table: "MaintenanceTickets",
                column: "DeliveredAt");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_RepairedAt",
                table: "MaintenanceTickets",
                column: "RepairedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_SoldAt",
                table: "Devices",
                column: "SoldAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentSettlements_SettlementDate",
                table: "ConsignmentSettlements",
                column: "SettlementDate");

            migrationBuilder.CreateIndex(
                name: "IX_Balances_ChargedAt",
                table: "Balances",
                column: "ChargedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_DeliveredAt",
                table: "MaintenanceTickets");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_RepairedAt",
                table: "MaintenanceTickets");

            migrationBuilder.DropIndex(
                name: "IX_Devices_SoldAt",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_ConsignmentSettlements_SettlementDate",
                table: "ConsignmentSettlements");

            migrationBuilder.DropIndex(
                name: "IX_Balances_ChargedAt",
                table: "Balances");
        }
    }
}
