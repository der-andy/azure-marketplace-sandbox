using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AzureMarketplaceSandbox.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTermUnitsToPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingTermUnit",
                table: "Plans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionTermUnit",
                table: "Plans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingTermUnit",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "SubscriptionTermUnit",
                table: "Plans");
        }
    }
}
