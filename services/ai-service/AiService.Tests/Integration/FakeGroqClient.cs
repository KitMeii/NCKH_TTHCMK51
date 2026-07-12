using AiService.Api.Groq;

namespace AiService.Tests.Integration;

/// <summary>Stands in for the real Groq API in tests — set <see cref="NextResponse"/> before each
/// call to control what "the model" returns, and read <see cref="CallCount"/> to assert caching
/// behavior (a cache hit means CallCount doesn't increase on a repeated identical request).</summary>
public sealed class FakeGroqClient : IGroqClient
{
    public string NextResponse { get; set; } = "OK";
    public int CallCount { get; private set; }

    public Task<string> CompleteAsync(IReadOnlyList<GroqMessage> messages, int maxTokens, CancellationToken ct)
    {
        CallCount++;
        return Task.FromResult(NextResponse);
    }
}
