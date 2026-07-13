using AdminService.Api.Clients;
using AdminService.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AdminService.Tests.Integration;

public sealed class AdminApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"admin-tests-{Guid.NewGuid()}";
    public readonly FakeAuthAdminClient AuthClient = new();
    public readonly FakeSystemStatsClient StatsClient = new();

    // See ContentApiFactory's static constructor for why this is needed (CI has no
    // appsettings.Development.json, so Program.cs throws on missing config before
    // ConfigureWebHost gets a chance to run).
    static AdminApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__AdminDb",
            Environment.GetEnvironmentVariable("ConnectionStrings__AdminDb") ?? "Server=unused;Database=unused;Trusted_Connection=True;TrustServerCertificate=True");
        Environment.SetEnvironmentVariable("Jwt__SigningKey",
            Environment.GetEnvironmentVariable("Jwt__SigningKey") ?? "dev-only-signing-key-do-not-use-in-production-min-32-chars");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:AutoMigrate", "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AdminDbContext>>();
            services.AddDbContext<AdminDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IAuthAdminClient>();
            services.AddSingleton<IAuthAdminClient>(AuthClient);

            services.RemoveAll<ISystemStatsClient>();
            services.AddSingleton<ISystemStatsClient>(StatsClient);
        });
    }
}
