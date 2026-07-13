using AdminService.Api.Clients;
using Shared.Infrastructure.Common;

namespace AdminService.Tests.Integration;

/// <summary>Stands in for auth-service (list users / change role) so tests don't need a live
/// instance. Seed <see cref="Users"/> before each test.</summary>
public sealed class FakeAuthAdminClient : IAuthAdminClient
{
    public List<RemoteUser> Users { get; } = [];

    public Task<IReadOnlyList<RemoteUser>> ListUsersAsync(string? role, CancellationToken ct)
    {
        IReadOnlyList<RemoteUser> result = string.IsNullOrWhiteSpace(role)
            ? Users.ToList()
            : Users.Where(u => u.Role == role).ToList();
        return Task.FromResult(result);
    }

    public Task<RemoteUser> ChangeRoleAsync(Guid userId, string newRole, CancellationToken ct)
    {
        var index = Users.FindIndex(u => u.Id == userId);
        if (index < 0)
        {
            throw new NotFoundException("Không tìm thấy người dùng.");
        }

        var updated = Users[index] with { Role = newRole };
        Users[index] = updated;
        return Task.FromResult(updated);
    }
}
