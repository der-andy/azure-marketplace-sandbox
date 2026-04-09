using AzureMarketplaceSandbox.Api;
using AzureMarketplaceSandbox.Api.Middleware;
using AzureMarketplaceSandbox.Auth;
using AzureMarketplaceSandbox.Components;
using AzureMarketplaceSandbox.Configuration;
using AzureMarketplaceSandbox.Data;
using AzureMarketplaceSandbox.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<SandboxOptions>(builder.Configuration.GetSection(SandboxOptions.SectionName));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<SeedDataOptions>(builder.Configuration.GetSection(SeedDataOptions.SectionName));

// Database
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "Sqlite";
builder.Services.AddDbContext<MarketplaceDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    if (dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

// Authentication
builder.Services.AddAuthentication(SandboxBearerHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SandboxBearerHandler>(SandboxBearerHandler.SchemeName, null);
builder.Services.AddAuthorization();

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
if (!dbProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

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
    Console.WriteLine($"   DB Provider:   {dbProvider}");
    Console.WriteLine("  ========================================");
    Console.WriteLine();
});

app.Run();

// Make Program accessible for WebApplicationFactory in tests
public partial class Program;

