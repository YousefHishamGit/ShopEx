using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopEx.Infrastructure.Persistence.ShopDb.Migrations
{
    /// <inheritdoc />
    public partial class AddHqPartnersToggle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "BranchPartnerSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "BranchPartnerSettings");
        }
    }
}
