using AiService.Api.Groq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiService.Tests.Integration;

public sealed class AiApiFactory : WebApplicationFactory<Program>
{
    public readonly FakeGroqClient GroqClient = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IGroqClient>();
            services.AddSingleton<IGroqClient>(GroqClient);
        });
    }
}
