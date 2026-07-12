using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuizService.Api.Data;
using QuizService.Api.Grading;
using QuizService.Api.Progress;

namespace QuizService.Tests.Integration;

public sealed class QuizApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"quiz-tests-{Guid.NewGuid()}";
    public readonly FakeProgressReporter ProgressReporter = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:AutoMigrate", "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<QuizDbContext>>();
            services.AddDbContext<QuizDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IOralGradingClient>();
            services.AddSingleton<IOralGradingClient, FakeOralGradingClient>();

            services.RemoveAll<IProgressReporter>();
            services.AddSingleton<IProgressReporter>(ProgressReporter);
        });
    }
}
