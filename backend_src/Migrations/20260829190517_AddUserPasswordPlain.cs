using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterprisePOS.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPasswordPlain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordPlain",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordPlain",
                table: "AspNetUsers");
        }
    }
}
