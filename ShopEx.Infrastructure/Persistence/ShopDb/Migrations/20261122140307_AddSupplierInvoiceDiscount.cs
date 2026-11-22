using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopEx.Infrastructure.Persistence.ShopDb.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierInvoiceDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "SupplierInvoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "SupplierInvoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                table: "SupplierInvoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                table: "SupplierInvoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Existing invoices had no discount: their subtotal equals their total
            migrationBuilder.Sql("UPDATE SupplierInvoices SET SubTotal = TotalAmount");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SupplierInvoices_DiscountAmount",
                table: "SupplierInvoices",
                sql: "[DiscountAmount] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SupplierInvoices_DiscountAmount",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "SubTotal",
                table: "SupplierInvoices");
        }
    }
}
