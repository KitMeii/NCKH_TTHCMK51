namespace Shared.Contracts;

/// <summary>HTTP header names shared between the gateway and downstream services.</summary>
public static class HeaderNames
{
    /// <summary>Correlation id generated/forwarded by the gateway, propagated through every downstream call and log line.</summary>
    public const string CorrelationId = "X-Correlation-Id";
}
