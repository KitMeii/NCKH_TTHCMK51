namespace AiService.Api.Groq;

/// <summary>
/// One platform-owned Groq API key, configured via env var (Groq__ApiKey) — never a per-user key.
/// This replaces the old design where every student stored their own Groq key in localStorage and
/// in a plaintext `profiles.groq_api_key` column that any teacher/admin could read (audit finding,
/// see [[project_audit_findings]]). Centralizing the key here is also what makes the gateway's
/// per-user rate limit on /api/v1/ai/** meaningful — it's protecting a real shared budget now.
/// </summary>
public sealed class GroqOptions
{
    public const string SectionName = "Groq";

    public required string ApiKey { get; init; }
    public string Model { get; init; } = "llama-3.3-70b-versatile";
    public string BaseUrl { get; init; } = "https://api.groq.com";
}
