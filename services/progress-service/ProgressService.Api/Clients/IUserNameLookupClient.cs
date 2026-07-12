namespace ProgressService.Api.Clients;

public sealed record UserName(Guid Id, string Name);

/// <summary>progress-service doesn't own user profile data — it calls auth-service's
/// GET /api/v1/auth/users/names to enrich a leaderboard with display names, rather than joining
/// across service boundaries.</summary>
public interface IUserNameLookupClient
{
    Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IReadOnlyList<Guid> userIds, CancellationToken ct);
}
