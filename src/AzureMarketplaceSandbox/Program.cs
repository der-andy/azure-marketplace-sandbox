using AzureMarketplaceSandbox.Api;
using AzureMarketplaceSandbox.Api.Middleware;
using AzureMarketplaceSandbox.Auth;
using AzureMarketplaceSandbox.Components;
using AzureMarketplaceSandbox.Configuration;
using AzureMarketplaceSandbox.Data;
using AzureMarketplaceSandbox.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<SandboxOptions>(builder.Configuration.GetSection(SandboxOptions.SectionName));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<SeedDataOptions>(builder.Configuration.GetSection(SeedDataOptions.SectionName));

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<MarketplaceDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

// Authentication — Entra ID (OIDC + Cookies) for Admin UI, SandboxBearer for API
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .Services
    .AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, SandboxBearerHandler>(SandboxBearerHandler.SchemeName, null);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiPolicy", policy =>
        policy.AddAuthenticationSchemes(SandboxBearerHandler.SchemeName)
              .RequireAuthenticatedUser());
});

// JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null; // Use JsonPropertyName attributes
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<OperationService>();
builder.Services.AddScoped<MeteringService>();
builder.Services.AddScoped<WebhookService>();
builder.Services.AddHttpClient("Webhook");
builder.Services.AddHostedService<SeedDataService>();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Apply pending migrations on startup (skip for InMemory provider in tests)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
    if (db.Database.IsSqlServer())
        db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// API middleware
app.UseMiddleware<ApiVersionMiddleware>();
app.UseMiddleware<RequestHeaderMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// API endpoints
app.MapFulfillmentSubscriptionApi();
app.MapFulfillmentOperationsApi();
app.MapMeteringApi();

// Authentication endpoints
app.MapGet("/authentication/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
    await context.SignOutAsync("Cookies");
}).AllowAnonymous();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization();

// Startup banner
var sandboxConfig = builder.Configuration.GetSection(SandboxOptions.SectionName).Get<SandboxOptions>() ?? new SandboxOptions();
app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine();
    Console.WriteLine("  ========================================");
    Console.WriteLine("   Azure Marketplace Sandbox");
    Console.WriteLine("  ========================================");
    Console.WriteLine($"   Admin UI:      {sandboxConfig.BaseUrl}");
    Console.WriteLine($"   API Base:      {sandboxConfig.BaseUrl}/api/saas/subscriptions");
    Console.WriteLine($"   Metering API:  {sandboxConfig.BaseUrl}/api/usageEvent");
    Console.WriteLine($"   Webhook URL:   {sandboxConfig.WebhookUrl}");
    Console.WriteLine($"   Landing Page:  {sandboxConfig.LandingPageUrl}");
    Console.WriteLine($"   Database:      {connectionString}");
    Console.WriteLine("  ========================================");
    Console.WriteLine();
});

app.Run();

// Make Program accessible for WebApplicationFactory in tests
public partial class Program;

