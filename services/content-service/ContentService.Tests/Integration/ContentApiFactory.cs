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
