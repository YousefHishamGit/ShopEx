using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopEx.Infrastructure.Persistence.ShopDb.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierReturnAndStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "StockBalances",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseID = table.Column<int>(type: "int", nullable: false),
                    RMSItemID = table.Column<int>(type: "int", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    AverageCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReorderLevel = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockBalances", x => x.ID);
                    table.CheckConstraint("CK_StockBalances_AverageCost", "[AverageCost] >= 0");
                    table.CheckConstraint("CK_StockBalances_QuantityOnHand", "[QuantityOnHand] >= 0");
                    table.ForeignKey(
                        name: "FK_StockBalances_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseID = table.Column<int>(type: "int", nullable: false),
                    RMSItemID = table.Column<int>(type: "int", nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReferenceID = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.ID);
                    table.CheckConstraint("CK_StockMovements_UnitCost", "[UnitCost] >= 0");
                    table.ForeignKey(
                        name: "FK_StockMovements_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturns",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RMSSupplierID = table.Column<int>(type: "int", nullable: false),
                    SupplierInvoiceID = table.Column<int>(type: "int", nullable: true),
                    WarehouseID = table.Column<int>(type: "int", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SettlementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierReturns", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_SupplierInvoices_SupplierInvoiceID",
                        column: x => x.SupplierInvoiceID,
                        principalTable: "SupplierInvoices",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierRefunds",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierReturnID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierRefunds", x => x.ID);
                    table.CheckConstraint("CK_SupplierRefunds_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_SupplierRefunds_SupplierReturns_SupplierReturnID",
                        column: x => x.SupplierReturnID,
                        principalTable: "SupplierReturns",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturnItems",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierReturnID = table.Column<int>(type: "int", nullable: false),
                    SupplierInvoiceItemID = table.Column<int>(type: "int", nullable: true),
                    RMSItemID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierReturnItems", x => x.ID);
                    table.CheckConstraint("CK_SupplierReturnItems_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_SupplierReturnItems_UnitPrice", "[UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_SupplierReturnItems_SupplierInvoiceItems_SupplierInvoiceItemID",
                        column: x => x.SupplierInvoiceItemID,
                        principalTable: "SupplierInvoiceItems",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturnItems_SupplierReturns_SupplierReturnID",
                        column: x => x.SupplierReturnID,
                        principalTable: "SupplierReturns",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockBalances_RMSItemID",
                table: "StockBalances",
                column: "RMSItemID");

            migrationBuilder.CreateIndex(
                name: "IX_StockBalances_WarehouseID_RMSItemID",
                table: "StockBalances",
                columns: new[] { "WarehouseID", "RMSItemID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MovementDate",
                table: "StockMovements",
                column: "MovementDate");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ReferenceType_ReferenceID",
                table: "StockMovements",
                columns: new[] { "ReferenceType", "ReferenceID" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_RMSItemID_WarehouseID",
                table: "StockMovements",
                columns: new[] { "RMSItemID", "WarehouseID" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_WarehouseID",
                table: "StockMovements",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierRefunds_RefundDate",
                table: "SupplierRefunds",
                column: "RefundDate");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierRefunds_SupplierReturnID",
                table: "SupplierRefunds",
                column: "SupplierReturnID");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnItems_RMSItemID",
                table: "SupplierReturnItems",
                column: "RMSItemID");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnItems_SupplierInvoiceItemID",
                table: "SupplierReturnItems",
                column: "SupplierInvoiceItemID");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnItems_SupplierReturnID",
                table: "SupplierReturnItems",
                column: "SupplierReturnID");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_ReturnNumber",
                table: "SupplierReturns",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_RMSSupplierID_ReturnDate",
                table: "SupplierReturns",
                columns: new[] { "RMSSupplierID", "ReturnDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_Status",
                table: "SupplierReturns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_SupplierInvoiceID",
                table: "SupplierReturns",
                column: "SupplierInvoiceID");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_WarehouseID",
                table: "SupplierReturns",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Name",
                table: "Warehouses",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockBalances");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "SupplierRefunds");

            migrationBuilder.DropTable(
                name: "SupplierReturnItems");

            migrationBuilder.DropTable(
                name: "SupplierReturns");

            migrationBuilder.DropTable(
                name: "Warehouses");
        }
    }
}
