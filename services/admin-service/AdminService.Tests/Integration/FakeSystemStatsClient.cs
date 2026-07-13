using AdminService.Api.Clients;

namespace AdminService.Tests.Integration;

public sealed class FakeSystemStatsClient : ISystemStatsClient
{
    public SystemOverview Overview { get; set; } = new(0, 0, 0);

    public Task<SystemOverview> GetOverviewAsync(CancellationToken ct) => Task.FromResult(Overview);
}
