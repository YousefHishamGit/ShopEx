using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopEx.Infrastructure.Persistence.ShopDb.Migrations
{
    /// <inheritdoc />
    public partial class AddConsignmentUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dealers",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dealers", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Consignments",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsignmentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DealerID = table.Column<int>(type: "int", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    AgreedDays = table.Column<int>(type: "int", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Open"),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consignments", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Consignments_Dealers_DealerID",
                        column: x => x.DealerID,
                        principalTable: "Dealers",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsignmentItems",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsignmentID = table.Column<int>(type: "int", nullable: false),
                    RMSItemID = table.Column<int>(type: "int", nullable: false),
                    QuantityIssued = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsignmentItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ConsignmentItems_Consignments_ConsignmentID",
                        column: x => x.ConsignmentID,
                        principalTable: "Consignments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsignmentPayments",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsignmentID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    PaymentMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsignmentPayments", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ConsignmentPayments_Consignments_ConsignmentID",
                        column: x => x.ConsignmentID,
                        principalTable: "Consignments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsignmentSettlements",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsignmentID = table.Column<int>(type: "int", nullable: false),
                    SettlementDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsignmentSettlements", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ConsignmentSettlements_Consignments_ConsignmentID",
                        column: x => x.ConsignmentID,
                        principalTable: "Consignments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsignmentSettlementItems",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsignmentSettlementID = table.Column<int>(type: "int", nullable: false),
                    ConsignmentItemID = table.Column<int>(type: "int", nullable: false),
                    QuantitySold = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantityReturned = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsignmentSettlementItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ConsignmentSettlementItems_ConsignmentItems_ConsignmentItemID",
                        column: x => x.ConsignmentItemID,
                        principalTable: "ConsignmentItems",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsignmentSettlementItems_ConsignmentSettlements_ConsignmentSettlementID",
                        column: x => x.ConsignmentSettlementID,
                        principalTable: "ConsignmentSettlements",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentItems_ConsignmentID",
                table: "ConsignmentItems",
                column: "ConsignmentID");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentPayments_ConsignmentID",
                table: "ConsignmentPayments",
                column: "ConsignmentID");

            migrationBuilder.CreateIndex(
                name: "IX_Consignments_ConsignmentNumber",
                table: "Consignments",
                column: "ConsignmentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consignments_DealerID",
                table: "Consignments",
                column: "DealerID");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentSettlementItems_ConsignmentItemID",
                table: "ConsignmentSettlementItems",
                column: "ConsignmentItemID");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentSettlementItems_ConsignmentSettlementID",
                table: "ConsignmentSettlementItems",
                column: "ConsignmentSettlementID");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentSettlements_ConsignmentID",
                table: "ConsignmentSettlements",
                column: "ConsignmentID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsignmentPayments");

            migrationBuilder.DropTable(
                name: "ConsignmentSettlementItems");

            migrationBuilder.DropTable(
                name: "ConsignmentItems");

            migrationBuilder.DropTable(
                name: "ConsignmentSettlements");

            migrationBuilder.DropTable(
                name: "Consignments");

            migrationBuilder.DropTable(
                name: "Dealers");
        }
    }
}
