using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopEx.Infrastructure.Persistence.ShopDb.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchPartners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BranchPartners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchPartners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BranchPartnerSettings",
                columns: table => new
                {
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    ZakatPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchPartnerSettings", x => x.StoreId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BranchPartners_StoreId",
                table: "BranchPartners",
                column: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchPartners");

            migrationBuilder.DropTable(
                name: "BranchPartnerSettings");
        }
    }
}
