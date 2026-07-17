using System.Security.Claims;
using AuthService.Api.Dtos;
using AuthService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Shared.Contracts;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Common;
using Shared.Infrastructure.Validation;

namespace AuthService.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, IAuthService authService, CancellationToken ct) =>
            {
                var result = await authService.RegisterAsync(request, ct);
                return Results.Created($"/api/v1/auth/me", ApiResponse<AuthResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<RegisterRequest>>()
            .AllowAnonymous();

        group.MapPost("/login", async (LoginRequest request, IAuthService authService, CancellationToken ct) =>
            {
                var result = await authService.LoginAsync(request, ct);
                return Results.Ok(ApiResponse<AuthResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<LoginRequest>>()
            .AllowAnonymous();

        group.MapGet("/me", async (ClaimsPrincipal principal, IAuthService authService, CancellationToken ct) =>
            {
                var result = await authService.GetByIdAsync(principal.GetUserId(), ct);
                return Results.Ok(ApiResponse<UserResponse>.Ok(result));
            })
            .RequireAuthorization();

        group.MapPut("/me", async (UpdateProfileRequest request, ClaimsPrincipal principal, IAuthService authService, CancellationToken ct) =>
            {
                var result = await authService.UpdateProfileAsync(principal.GetUserId(), request, ct);
                return Results.Ok(ApiResponse<UserResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<UpdateProfileRequest>>()
            .RequireAuthorization();

        // Cross-service display enrichment (progress-service leaderboard, admin-service roster) —
        // name only, see UserNameResponse remarks. Any authenticated caller, not just Teacher/Admin,
        // since a student's own leaderboard view needs classmates' names too.
        group.MapGet("/users/names", async (string ids, IAuthService authService, CancellationToken ct) =>
            {
                var parsedIds = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse)
                    .ToList();
                var result = await authService.GetNamesByIdsAsync(parsedIds, ct);
                return Results.Ok(ApiResponse<IReadOnlyList<UserNameResponse>>.Ok(result));
            })
            .RequireAuthorization();

        // Admin-only account management. Intended caller is admin-service (which audits every
        // change) — see the remarks on admin-service's HttpAuthAdminClient. [Authorize(Roles=Admin)]
        // alone is not enough: the gateway proxies every sub-path under /api/v1/auth/**, so any
        // client with a valid Admin JWT could otherwise call these directly and bypass
        // admin-service's audit log. RequireInternalServiceKeyFilter closes that: only a caller
        // that also knows InternalService:SharedKey (admin-service) gets through.
        group.MapGet("/users", async (string? role, IAuthService authService, CancellationToken ct) =>
            {
                var result = await authService.ListUsersAsync(role, ct);
                return Results.Ok(ApiResponse<IReadOnlyList<UserResponse>>.Ok(result));
            })
            .AddEndpointFilter<RequireInternalServiceKeyFilter>()
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        group.MapPut("/users/{id:guid}/role", async (Guid id, ChangeRoleRequest request, ClaimsPrincipal principal, IAuthService authService, CancellationToken ct) =>
            {
                if (principal.GetUserId() == id)
                {
                    throw new ConflictException("Không thể tự đổi role của chính mình.");
                }

                var result = await authService.ChangeRoleAsync(id, request.Role, ct);
                return Results.Ok(ApiResponse<UserResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<ChangeRoleRequest>>()
            .AddEndpointFilter<RequireInternalServiceKeyFilter>()
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        return app;
    }
}
