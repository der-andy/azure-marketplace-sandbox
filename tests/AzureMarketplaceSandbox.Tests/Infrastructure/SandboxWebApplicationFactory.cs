using AzureMarketplaceSandbox.Auth;
using AzureMarketplaceSandbox.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AzureMarketplaceSandbox.Tests.Infrastructure;

public class SandboxWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide dummy AzureAd config so OIDC middleware does not throw
        builder.UseSetting("AzureAd:TenantId", "00000000-0000-0000-0000-000000000000");
        builder.UseSetting("AzureAd:ClientId", "00000000-0000-0000-0000-000000000000");

        builder.ConfigureServices(services =>
        {
            // Remove all DbContext-related registrations
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<MarketplaceDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true)
                .ToList();
            foreach (var d in descriptors)
                services.Remove(d);

            // Re-register with InMemory provider
            services.AddDbContext<MarketplaceDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            // Use SandboxBearer as default scheme so API tests are not affected by OIDC
            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = SandboxBearerHandler.SchemeName;
                options.DefaultChallengeScheme = SandboxBearerHandler.SchemeName;
            });
        });

        builder.UseEnvironment("Development");
    }

    public async Task SeedAsync(Func<MarketplaceDbContext, Task> seedAction)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        await db.Database.EnsureCreatedAsync();
        await seedAction(db);
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-token");
        return client;
    }
}
