using ContentService.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ContentService.Tests.Integration;

public sealed class ContentApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"content-tests-{Guid.NewGuid()}";

    // Program.cs reads ConnectionStrings:ContentDb and Jwt:SigningKey off the default config
    // providers (env vars, appsettings.json) before ConfigureWebHost below ever runs — on a
    // checkout without a local appsettings.Development.json (gitignored; every CI run included),
    // it throws before we get a chance to swap in InMemory. Seed both as process env vars, only
    // if not already set. The connection string value is irrelevant (always replaced below); the
    // signing key must match TestTokens.cs so tokens minted there validate against this host.
    static ContentApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__ContentDb",
            Environment.GetEnvironmentVariable("ConnectionStrings__ContentDb") ?? "Server=unused;Database=unused;Trusted_Connection=True;TrustServerCertificate=True");
        Environment.SetEnvironmentVariable("Jwt__SigningKey",
            Environment.GetEnvironmentVariable("Jwt__SigningKey") ?? "dev-only-signing-key-do-not-use-in-production-min-32-chars");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:AutoMigrate", "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ContentDbContext>>();
            services.AddDbContext<ContentDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
