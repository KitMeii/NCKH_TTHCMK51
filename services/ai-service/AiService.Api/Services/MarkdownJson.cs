namespace AiService.Api.Services;

/// <summary>LLMs frequently wrap JSON answers in a ```json ... ``` fence even when told not to —
/// strip it before deserializing.</summary>
internal static class MarkdownJson
{
    public static string StripCodeFence(string text)
    {
        if (!text.StartsWith("```", StringComparison.Ordinal))
        {
            return text;
        }

        var firstNewline = text.IndexOf('\n');
        var withoutOpenFence = firstNewline >= 0 ? text[(firstNewline + 1)..] : text;
        var closingFenceIndex = withoutOpenFence.LastIndexOf("```", StringComparison.Ordinal);
        return closingFenceIndex >= 0 ? withoutOpenFence[..closingFenceIndex].Trim() : withoutOpenFence.Trim();
    }
}
