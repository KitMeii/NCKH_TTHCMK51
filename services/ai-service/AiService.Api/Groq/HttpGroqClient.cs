using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace AiService.Api.Groq;

public sealed class HttpGroqClient : IGroqClient
{
    private readonly HttpClient _httpClient;
    private readonly GroqOptions _options;

    public HttpGroqClient(HttpClient httpClient, IOptions<GroqOptions> options)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<string> CompleteAsync(IReadOnlyList<GroqMessage> messages, int maxTokens, CancellationToken ct)
    {
        var request = new GroqChatRequest(_options.Model, messages, maxTokens);
        var response = await _httpClient.PostAsJsonAsync("/openai/v1/chat/completions", request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<GroqChatResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Groq returned an empty response.");

        var content = body.Choices.FirstOrDefault()?.Message.Content
            ?? throw new InvalidOperationException("Groq response had no choices.");

        return content;
    }

    private sealed record GroqChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<GroqMessage> Messages,
        [property: JsonPropertyName("max_tokens")] int MaxTokens);

    private sealed record GroqChatResponse([property: JsonPropertyName("choices")] IReadOnlyList<GroqChoice> Choices);

    private sealed record GroqChoice([property: JsonPropertyName("message")] GroqResponseMessage Message);

    private sealed record GroqResponseMessage([property: JsonPropertyName("content")] string Content);
}
