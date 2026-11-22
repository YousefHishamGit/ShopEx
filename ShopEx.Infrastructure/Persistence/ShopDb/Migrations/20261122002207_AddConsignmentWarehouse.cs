using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopEx.Infrastructure.Persistence.ShopDb.Migrations
{
    /// <inheritdoc />
    public partial class AddConsignmentWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Consignments_DealerID",
                table: "Consignments");

            migrationBuilder.AddColumn<int>(
                name: "WarehouseID",
                table: "Consignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consignments_DealerID_Status",
                table: "Consignments",
                columns: new[] { "DealerID", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Consignments_IssueDate",
                table: "Consignments",
                column: "IssueDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Consignments_DealerID_Status",
                table: "Consignments");

            migrationBuilder.DropIndex(
                name: "IX_Consignments_IssueDate",
                table: "Consignments");

            migrationBuilder.DropColumn(
                name: "WarehouseID",
                table: "Consignments");

            migrationBuilder.CreateIndex(
                name: "IX_Consignments_DealerID",
                table: "Consignments",
                column: "DealerID");
        }
    }
}
