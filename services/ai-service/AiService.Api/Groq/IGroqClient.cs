namespace AiService.Api.Groq;

public sealed record GroqMessage(string Role, string Content);

public interface IGroqClient
{
    Task<string> CompleteAsync(IReadOnlyList<GroqMessage> messages, int maxTokens, CancellationToken ct);
}
