using AiService.Api.Groq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiService.Tests.Integration;

public sealed class AiApiFactory : WebApplicationFactory<Program>
{
    public readonly FakeGroqClient GroqClient = new();

    // Program.cs reads Jwt:SigningKey off the default config providers (env vars,
    // appsettings.json) before ConfigureWebHost below ever runs — on a checkout without a local
    // appsettings.Development.json (gitignored; every CI run included), it throws before this
    // factory gets a chance to configure anything. Seed it as a process env var, only if not
    // already set, matching the value TestTokens.cs signs with (ai-service has no database, so
    // no ConnectionStrings entry is needed here).
    static AiApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__SigningKey",
            Environment.GetEnvironmentVariable("Jwt__SigningKey") ?? "dev-only-signing-key-do-not-use-in-production-min-32-chars");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IGroqClient>();
            services.AddSingleton<IGroqClient>(GroqClient);
        });
    }
}
