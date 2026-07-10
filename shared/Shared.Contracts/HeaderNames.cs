namespace Shared.Contracts;

/// <summary>HTTP header names shared between the gateway and downstream services.</summary>
public static class HeaderNames
{
    /// <summary>Correlation id generated/forwarded by the gateway, propagated through every downstream call and log line.</summary>
    public const string CorrelationId = "X-Correlation-Id";

    /// <summary>User id (JWT sub claim) forwarded by the gateway once it has validated the caller's JWT.
    /// Downstream services can trust this header only because the network topology guarantees they are
    /// only reachable through the gateway (docker-compose internal network) — never expose a service
    /// directly to the internet without re-validating the JWT itself.</summary>
    public const string UserId = "X-User-Id";

    /// <summary>Role claim forwarded by the gateway — see remarks on <see cref="UserId"/> about the trust boundary.</summary>
    public const string UserRole = "X-User-Role";
}
