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
