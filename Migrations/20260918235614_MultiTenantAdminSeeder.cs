using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.API.Migrations
{
    /// <inheritdoc />
    public partial class MultiTenantAdminSeeder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "AdminUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "AdminUsers");
        }
    }
}
