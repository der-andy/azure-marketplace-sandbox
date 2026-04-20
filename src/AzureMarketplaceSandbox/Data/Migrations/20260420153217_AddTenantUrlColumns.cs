using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AzureMarketplaceSandbox.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantUrlColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LandingPageUrl",
                table: "Tenants",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WebhookUrl",
                table: "Tenants",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LandingPageUrl",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "WebhookUrl",
                table: "Tenants");
        }
    }
}
