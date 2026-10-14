using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterprisePOS.Migrations
{
    /// <inheritdoc />
    public partial class AddDealProductQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "DealProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "DealProducts");
        }
    }
}
