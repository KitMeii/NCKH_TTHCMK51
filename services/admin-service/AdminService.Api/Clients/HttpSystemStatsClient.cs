using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Shared.Infrastructure.Common;

namespace AdminService.Api.Clients;

public sealed class HttpSystemStatsClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor) : ISystemStatsClient
{
    private sealed record IdOnly(Guid Id);

    public async Task<SystemOverview> GetOverviewAsync(CancellationToken ct)
    {
        var materials = await CountAsync("content-service", "/api/v1/content/materials", ct);
        var questions = await CountAsync("quiz-service", "/api/v1/quiz/questions", ct);
        var oralQuestions = await CountAsync("quiz-service", "/api/v1/quiz/oral-questions", ct);

        return new SystemOverview(materials, questions, oralQuestions);
    }

    private async Task<int> CountAsync(string clientName, string url, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(clientName);

        using var message = new HttpRequestMessage(HttpMethod.Get, url);
        var incomingAuth = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(incomingAuth) && AuthenticationHeaderValue.TryParse(incomingAuth, out var parsed))
        {
            message.Headers.Authorization = parsed;
        }

        var response = await client.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<IdOnly>>>(cancellationToken: ct);
        return body?.Data?.Count ?? 0;
    }
}
