using System.Net.Http.Json;
using Shared.Infrastructure.Common;

namespace QuizService.Api.Grading;

public sealed class HttpOralGradingClient(HttpClient httpClient) : IOralGradingClient
{
    public async Task<OralGradingResult> GradeAsync(OralGradingRequest request, CancellationToken ct)
    {
        var response = await httpClient.PostAsJsonAsync("/api/v1/ai/grade-oral", request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<OralGradingResult>>(cancellationToken: ct)
            ?? throw new InvalidOperationException("ai-service returned an empty grading response.");

        return body.Data ?? throw new InvalidOperationException("ai-service returned a successful response with no grading data.");
    }
}
