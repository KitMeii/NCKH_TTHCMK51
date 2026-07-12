using ProgressService.Api.Clients;

namespace ProgressService.Tests.Integration;

/// <summary>Stands in for auth-service (called for leaderboard name enrichment) so tests don't
/// need a live auth-service instance.</summary>
public sealed class FakeUserNameLookupClient : IUserNameLookupClient
{
    public Dictionary<Guid, string> Names { get; } = new();

    public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IReadOnlyList<Guid> userIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(
            userIds.Where(Names.ContainsKey).ToDictionary(id => id, id => Names[id]));
}
