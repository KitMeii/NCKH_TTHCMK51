using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProgressService.Api.Clients;
using ProgressService.Api.Data;

namespace ProgressService.Tests.Integration;

public sealed class ProgressApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"progress-tests-{Guid.NewGuid()}";
    public readonly FakeUserNameLookupClient NameLookup = new();

    // See ContentApiFactory's static constructor for why this is needed (CI has no
    // appsettings.Development.json, so Program.cs throws on missing config before
    // ConfigureWebHost gets a chance to run).
    static ProgressApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__ProgressDb",
            Environment.GetEnvironmentVariable("ConnectionStrings__ProgressDb") ?? "Server=unused;Database=unused;Trusted_Connection=True;TrustServerCertificate=True");
        Environment.SetEnvironmentVariable("Jwt__SigningKey",
            Environment.GetEnvironmentVariable("Jwt__SigningKey") ?? "dev-only-signing-key-do-not-use-in-production-min-32-chars");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:AutoMigrate", "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ProgressDbContext>>();
            services.AddDbContext<ProgressDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IUserNameLookupClient>();
            services.AddSingleton<IUserNameLookupClient>(NameLookup);
        });
    }
}
