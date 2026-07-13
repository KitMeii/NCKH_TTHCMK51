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
