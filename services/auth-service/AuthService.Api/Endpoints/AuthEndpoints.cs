using System.Security.Claims;
using AuthService.Api.Dtos;
using AuthService.Api.Services;
using Microsoft.AspNetCore.Authorization;
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
                var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? principal.FindFirstValue("sub")!);
                var result = await authService.GetByIdAsync(userId, ct);
                return Results.Ok(ApiResponse<UserResponse>.Ok(result));
            })
            .RequireAuthorization();

        return app;
    }
}
