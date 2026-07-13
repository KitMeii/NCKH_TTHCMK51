using AdminService.Api.Clients;
using AdminService.Api.Data;
using AdminService.Api.Dtos;
using AdminService.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Common;

namespace AdminService.Api.Services;

public sealed class UserManagementService(IAuthAdminClient authClient, AdminDbContext db) : IUserManagementService
{
    public async Task<IReadOnlyList<UserSummaryResponse>> ListUsersAsync(string? role, CancellationToken ct)
    {
        var users = await authClient.ListUsersAsync(role, ct);
        return users.Select(u => new UserSummaryResponse(u.Id, u.Email, u.Name, u.Role)).ToList();
    }

    public async Task<UserSummaryResponse> ChangeRoleAsync(Guid adminUserId, Guid targetUserId, string newRole, CancellationToken ct)
    {
        var allUsers = await authClient.ListUsersAsync(null, ct);
        var target = allUsers.FirstOrDefault(u => u.Id == targetUserId)
            ?? throw new NotFoundException("Không tìm thấy người dùng.");

        var updated = await authClient.ChangeRoleAsync(targetUserId, newRole, ct);

        db.RoleChangeAudits.Add(new RoleChangeAudit
        {
            AdminUserId = adminUserId,
            TargetUserId = targetUserId,
            OldRole = target.Role,
            NewRole = newRole,
        });
        await db.SaveChangesAsync(ct);

        return new UserSummaryResponse(updated.Id, updated.Email, updated.Name, updated.Role);
    }

    public async Task<IReadOnlyList<RoleChangeAuditResponse>> GetAuditLogAsync(int top, CancellationToken ct)
    {
        return await db.RoleChangeAudits
            .OrderByDescending(a => a.ChangedAtUtc)
            .Take(top)
            .Select(a => new RoleChangeAuditResponse(a.Id, a.AdminUserId, a.TargetUserId, a.OldRole, a.NewRole, a.ChangedAtUtc))
            .ToListAsync(ct);
    }
}
