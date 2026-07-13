namespace AdminService.Api.Clients;

public sealed record SystemOverview(int MaterialCount, int QuestionCount, int OralQuestionCount);

/// <summary>Composes a system overview from other services' existing list endpoints (counting
/// the results) rather than each service exposing a bespoke count endpoint — fine at this
/// project's scale; revisit with dedicated aggregate endpoints if the lists get large.</summary>
public interface ISystemStatsClient
{
    Task<SystemOverview> GetOverviewAsync(CancellationToken ct);
}
