using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AzureMarketplaceSandbox.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AadInfo",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    EmailId = table.Column<string>(nullable: false),
                    ObjectId = table.Column<string>(nullable: false),
                    TenantId = table.Column<string>(nullable: false),
                    Puid = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AadInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceTokens",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    Token = table.Column<string>(maxLength: 256, nullable: false),
                    SubscriptionId = table.Column<Guid>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    ExpiresAt = table.Column<DateTime>(nullable: false),
                    IsResolved = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    OfferId = table.Column<string>(maxLength: 128, nullable: false),
                    PublisherId = table.Column<string>(nullable: false),
                    DisplayName = table.Column<string>(nullable: false),
                    Currency = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Operations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    OperationId = table.Column<Guid>(nullable: false),
                    ActivityId = table.Column<Guid>(nullable: false),
                    SubscriptionId = table.Column<Guid>(nullable: false),
                    OfferId = table.Column<string>(nullable: false),
                    PublisherId = table.Column<string>(nullable: false),
                    PlanId = table.Column<string>(nullable: false),
                    Quantity = table.Column<int>(nullable: true),
                    Action = table.Column<string>(nullable: false),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    Status = table.Column<string>(nullable: false),
                    ErrorStatusCode = table.Column<string>(nullable: false),
                    ErrorMessage = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionTerm",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    StartDate = table.Column<DateTime>(nullable: true),
                    EndDate = table.Column<DateTime>(nullable: true),
                    TermUnit = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionTerm", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsageEvents",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    UsageEventId = table.Column<Guid>(nullable: false),
                    Status = table.Column<string>(nullable: false),
                    MessageTime = table.Column<DateTime>(nullable: false),
                    ResourceId = table.Column<Guid>(nullable: false),
                    Quantity = table.Column<decimal>(nullable: false),
                    Dimension = table.Column<string>(nullable: false),
                    EffectiveStartTime = table.Column<DateTime>(nullable: false),
                    PlanId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookDeliveryLogs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    WebhookDeliveryLogId = table.Column<Guid>(nullable: false),
                    SubscriptionId = table.Column<Guid>(nullable: false),
                    Action = table.Column<string>(nullable: false),
                    PayloadJson = table.Column<string>(nullable: false),
                    ResponseStatusCode = table.Column<int>(nullable: true),
                    ResponseBody = table.Column<string>(nullable: true),
                    ErrorMessage = table.Column<string>(nullable: true),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    Success = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeliveryLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeteringDimensions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    DimensionId = table.Column<string>(nullable: false),
                    PricePerUnit = table.Column<decimal>(nullable: false),
                    UnitOfMeasure = table.Column<string>(nullable: false),
                    DisplayName = table.Column<string>(nullable: false),
                    OfferId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeteringDimensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeteringDimensions_Offers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "Offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    PlanId = table.Column<string>(nullable: false),
                    DisplayName = table.Column<string>(nullable: false),
                    IsPrivate = table.Column<bool>(nullable: false),
                    Description = table.Column<string>(nullable: false),
                    MinQuantity = table.Column<int>(nullable: false),
                    MaxQuantity = table.Column<int>(nullable: false),
                    HasFreeTrials = table.Column<bool>(nullable: false),
                    IsPricePerSeat = table.Column<bool>(nullable: false),
                    IsStopSell = table.Column<bool>(nullable: false),
                    Market = table.Column<string>(nullable: false),
                    BillingTermUnit = table.Column<string>(nullable: false),
                    SubscriptionTermUnit = table.Column<string>(nullable: false),
                    Price = table.Column<decimal>(nullable: false),
                    TermDescription = table.Column<string>(nullable: false),
                    OfferId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plans_Offers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "Offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    SubscriptionId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    PublisherId = table.Column<string>(nullable: false),
                    OfferId = table.Column<string>(nullable: false),
                    PlanId = table.Column<string>(nullable: false),
                    Quantity = table.Column<int>(nullable: true),
                    BeneficiaryId = table.Column<int>(nullable: false),
                    PurchaserId = table.Column<int>(nullable: false),
                    SessionMode = table.Column<string>(nullable: false),
                    IsFreeTrial = table.Column<bool>(nullable: false),
                    IsTest = table.Column<bool>(nullable: false),
                    SandboxType = table.Column<string>(nullable: false),
                    SaasSubscriptionStatus = table.Column<string>(nullable: false),
                    TermId = table.Column<int>(nullable: false),
                    AutoRenew = table.Column<bool>(nullable: false),
                    Created = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_AadInfo_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "AadInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_AadInfo_PurchaserId",
                        column: x => x.PurchaserId,
                        principalTable: "AadInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_SubscriptionTerm_TermId",
                        column: x => x.TermId,
                        principalTable: "SubscriptionTerm",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanMeteringDimensions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    PlanId = table.Column<int>(nullable: false),
                    MeteringDimensionId = table.Column<int>(nullable: false),
                    IncludedQuantity = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanMeteringDimensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanMeteringDimensions_MeteringDimensions_MeteringDimensionId",
                        column: x => x.MeteringDimensionId,
                        principalTable: "MeteringDimensions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanMeteringDimensions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceTokens_Token",
                table: "MarketplaceTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeteringDimensions_OfferId",
                table: "MeteringDimensions",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_OfferId",
                table: "Offers",
                column: "OfferId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanMeteringDimensions_MeteringDimensionId",
                table: "PlanMeteringDimensions",
                column: "MeteringDimensionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanMeteringDimensions_PlanId",
                table: "PlanMeteringDimensions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_OfferId",
                table: "Plans",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_BeneficiaryId",
                table: "Subscriptions",
                column: "BeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PurchaserId",
                table: "Subscriptions",
                column: "PurchaserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TermId",
                table: "Subscriptions",
                column: "TermId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketplaceTokens");

            migrationBuilder.DropTable(
                name: "Operations");

            migrationBuilder.DropTable(
                name: "PlanMeteringDimensions");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "UsageEvents");

            migrationBuilder.DropTable(
                name: "WebhookDeliveryLogs");

            migrationBuilder.DropTable(
                name: "MeteringDimensions");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropTable(
                name: "AadInfo");

            migrationBuilder.DropTable(
                name: "SubscriptionTerm");

            migrationBuilder.DropTable(
                name: "Offers");
        }
    }
}
