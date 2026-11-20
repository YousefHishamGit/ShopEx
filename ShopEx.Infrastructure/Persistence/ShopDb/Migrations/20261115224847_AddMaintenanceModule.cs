using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopEx.Infrastructure.Persistence.ShopDb.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaintenanceTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReceivedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CustomerPhone = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IssueDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Accessories = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RepairedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RepairNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTickets", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceTickets_ExpectedCost", "[ExpectedCost] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "MaintenancePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaintenanceTicketId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenancePayments", x => x.Id);
                    table.CheckConstraint("CK_MaintenancePayments_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_MaintenancePayments_MaintenanceTickets_MaintenanceTicketId",
                        column: x => x.MaintenanceTicketId,
                        principalTable: "MaintenanceTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceTicketOperations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaintenanceTicketId = table.Column<int>(type: "int", nullable: false),
                    OperationName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTicketOperations", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceTicketOperations_Cost", "[Cost] >= 0");
                    table.ForeignKey(
                        name: "FK_MaintenanceTicketOperations_MaintenanceTickets_MaintenanceTicketId",
                        column: x => x.MaintenanceTicketId,
                        principalTable: "MaintenanceTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePayments_MaintenanceTicketId",
                table: "MaintenancePayments",
                column: "MaintenanceTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePayments_PaidAt",
                table: "MaintenancePayments",
                column: "PaidAt");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTicketOperations_MaintenanceTicketId",
                table: "MaintenanceTicketOperations",
                column: "MaintenanceTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_CustomerPhone",
                table: "MaintenanceTickets",
                column: "CustomerPhone");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_ReceivedAt",
                table: "MaintenanceTickets",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_Status",
                table: "MaintenanceTickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_TicketNumber",
                table: "MaintenanceTickets",
                column: "TicketNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaintenancePayments");

            migrationBuilder.DropTable(
                name: "MaintenanceTicketOperations");

            migrationBuilder.DropTable(
                name: "MaintenanceTickets");
        }
    }
}
