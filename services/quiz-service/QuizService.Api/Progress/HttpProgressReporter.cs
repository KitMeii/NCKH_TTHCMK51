using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace QuizService.Api.Progress;

public sealed class HttpProgressReporter(HttpClient httpClient, IHttpContextAccessor httpContextAccessor) : IProgressReporter
{
    private sealed record RecordScoreRequest(decimal Score);

    public async Task ReportScoreAsync(decimal score, CancellationToken ct)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/progress/record-score")
        {
            Content = JsonContent.Create(new RecordScoreRequest(score)),
        };

        var incomingAuth = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(incomingAuth) && AuthenticationHeaderValue.TryParse(incomingAuth, out var parsed))
        {
            message.Headers.Authorization = parsed;
        }

        var response = await httpClient.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();
    }
}
