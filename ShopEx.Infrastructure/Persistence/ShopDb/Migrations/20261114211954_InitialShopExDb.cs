using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopEx.Infrastructure.Persistence.ShopDb.Migrations
{
    /// <inheritdoc />
    public partial class InitialShopExDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierInvoices",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RMSSupplierID = table.Column<int>(type: "int", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SupplierInvoiceStatus = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PaymentType = table.Column<int>(type: "int", nullable: false),
                    CreditDays = table.Column<int>(type: "int", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierInvoices", x => x.ID);
                    table.CheckConstraint("CK_SupplierInvoices_CreditDays", "[CreditDays] IS NULL OR [CreditDays] > 0");
                    table.CheckConstraint("CK_SupplierInvoices_TotalAmount", "[TotalAmount] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "SupplierInvoiceItems",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierInvoiceID = table.Column<int>(type: "int", nullable: false),
                    RMSItemID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierInvoiceItems", x => x.ID);
                    table.CheckConstraint("CK_SupplierInvoiceItems_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_SupplierInvoiceItems_UnitPrice", "[UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_SupplierInvoiceItems_SupplierInvoices_SupplierInvoiceID",
                        column: x => x.SupplierInvoiceID,
                        principalTable: "SupplierInvoices",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierPayments",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierInvoiceID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPayments", x => x.ID);
                    table.CheckConstraint("CK_SupplierPayments_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_SupplierPayments_SupplierInvoices_SupplierInvoiceID",
                        column: x => x.SupplierInvoiceID,
                        principalTable: "SupplierInvoices",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceItems_RMSItemID",
                table: "SupplierInvoiceItems",
                column: "RMSItemID");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceItems_SupplierInvoiceID",
                table: "SupplierInvoiceItems",
                column: "SupplierInvoiceID");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_InvoiceDate",
                table: "SupplierInvoices",
                column: "InvoiceDate");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_RMSSupplierID_InvoiceNumber",
                table: "SupplierInvoices",
                columns: new[] { "RMSSupplierID", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_SupplierInvoiceStatus_DueDate",
                table: "SupplierInvoices",
                columns: new[] { "SupplierInvoiceStatus", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_PaymentDate",
                table: "SupplierPayments",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_SupplierInvoiceID",
                table: "SupplierPayments",
                column: "SupplierInvoiceID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierInvoiceItems");

            migrationBuilder.DropTable(
                name: "SupplierPayments");

            migrationBuilder.DropTable(
                name: "SupplierInvoices");
        }
    }
}
