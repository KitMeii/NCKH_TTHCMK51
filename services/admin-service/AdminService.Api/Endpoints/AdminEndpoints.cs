using System.Security.Claims;
using AdminService.Api.Dtos;
using AdminService.Api.Services;
using Shared.Contracts;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Common;
using Shared.Infrastructure.Validation;

namespace AdminService.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // Every route here requires Admin — the gateway already double-checks this on
        // /api/v1/admin/**, but this service enforces it independently too (same convention as
        // every other service in this codebase: never trust the gateway alone).
        var group = app.MapGroup("/api/v1/admin").WithTags("Admin").RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        group.MapGet("/users", async (string? role, IUserManagementService service, CancellationToken ct) =>
        {
            var result = await service.ListUsersAsync(role, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<UserSummaryResponse>>.Ok(result));
        });

        group.MapPut("/users/{id:guid}/role", async (Guid id, ChangeRoleRequest request, ClaimsPrincipal principal, IUserManagementService service, CancellationToken ct) =>
            {
                var result = await service.ChangeRoleAsync(principal.GetUserId(), id, request.Role, ct);
                return Results.Ok(ApiResponse<UserSummaryResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<ChangeRoleRequest>>();

        group.MapGet("/audit-log", async (int? top, IUserManagementService service, CancellationToken ct) =>
        {
            var result = await service.GetAuditLogAsync(top is > 0 and <= 200 ? top.Value : 50, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<RoleChangeAuditResponse>>.Ok(result));
        });

        group.MapGet("/config", async (ISystemConfigService service, CancellationToken ct) =>
        {
            var result = await service.GetAllAsync(ct);
            return Results.Ok(ApiResponse<IReadOnlyList<SystemConfigResponse>>.Ok(result));
        });

        group.MapPut("/config/{key}", async (string key, SetConfigRequest request, ClaimsPrincipal principal, ISystemConfigService service, CancellationToken ct) =>
            {
                var result = await service.SetAsync(key, request.Value, principal.GetUserId(), ct);
                return Results.Ok(ApiResponse<SystemConfigResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<SetConfigRequest>>();

        group.MapGet("/stats/overview", async (ISystemOverviewService service, CancellationToken ct) =>
        {
            var result = await service.GetOverviewAsync(ct);
            return Results.Ok(ApiResponse<SystemOverviewResponse>.Ok(result));
        });

        return app;
    }
}
