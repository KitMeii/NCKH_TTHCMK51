using AdminService.Api.Dtos;

namespace AdminService.Api.Services;

public interface IUserManagementService
{
    Task<IReadOnlyList<UserSummaryResponse>> ListUsersAsync(string? role, CancellationToken ct);
    Task<UserSummaryResponse> ChangeRoleAsync(Guid adminUserId, Guid targetUserId, string newRole, CancellationToken ct);
    Task<IReadOnlyList<RoleChangeAuditResponse>> GetAuditLogAsync(int top, CancellationToken ct);
}
