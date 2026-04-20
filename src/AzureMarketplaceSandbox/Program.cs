using AzureMarketplaceSandbox.Api;
using AzureMarketplaceSandbox.Api.Middleware;
using AzureMarketplaceSandbox.Auth;
using AzureMarketplaceSandbox.Components;
using AzureMarketplaceSandbox.Configuration;
using AzureMarketplaceSandbox.Data;
using AzureMarketplaceSandbox.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, SandboxBearerHandler>(SandboxBearerHandler.SchemeName, null)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// Route authentication by path: /api → SandboxBearer, everything else → Cookies (OIDC)
builder.Services.PostConfigure<AuthenticationOptions>(options =>
{
    options.DefaultScheme = "RouteSelector";
    options.DefaultChallengeScheme = "RouteSelector";
    options.DefaultAuthenticateScheme = "RouteSelector";
});
builder.Services.AddAuthentication()
    .AddPolicyScheme("RouteSelector", "RouteSelector", options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Path.StartsWithSegments("/api")
                ? SandboxBearerHandler.SchemeName
                : CookieAuthenticationDefaults.AuthenticationScheme;
    });

// Ensure cookie challenges redirect to Entra ID (OIDC) instead of /Account/Login
builder.Services.PostConfigure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options => options.ForwardChallenge = OpenIdConnectDefaults.AuthenticationScheme);

builder.Services.AddAuthorization();

// JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null; // Use JsonPropertyName attributes
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Services
builder.Services.AddScoped<ITenantContext, TenantContext>();
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
// Return JSON for unhandled exceptions on API paths instead of HTML error pages
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            if (app.Environment.IsDevelopment() && error?.Error is { } ex)
            {
                await context.Response.WriteAsJsonAsync(new
                {
                    message = ex.Message,
                    code = "InternalServerError",
                    type = ex.GetType().FullName,
                    stackTrace = ex.StackTrace
                });
            }
            else
            {
                await context.Response.WriteAsJsonAsync(new { message = "An internal server error occurred.", code = "InternalServerError" });
            }
        }
        else
        {
            context.Response.Redirect("/Error");
        }
    });
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Disable status code pages for API paths so 401/404 are returned as-is
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodePagesFeature>();
        if (feature is not null)
            feature.Enabled = false;
    }
    await next();
});

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

