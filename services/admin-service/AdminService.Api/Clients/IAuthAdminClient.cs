namespace AdminService.Api.Clients;

public sealed record RemoteUser(Guid Id, string Email, string Name, string Role);

/// <summary>admin-service owns no user data itself — every read/write goes to auth-service, which
/// owns the Users table. See AuthService.Api/Endpoints/AuthEndpoints.cs for the two admin-only
/// endpoints this calls (GET /users, PUT /users/{id}/role).</summary>
public interface IAuthAdminClient
{
    Task<IReadOnlyList<RemoteUser>> ListUsersAsync(string? role, CancellationToken ct);
    Task<RemoteUser> ChangeRoleAsync(Guid userId, string newRole, CancellationToken ct);
}
