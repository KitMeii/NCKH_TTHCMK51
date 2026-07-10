using System.Security.Claims;
using ContentService.Api.Dtos;
using ContentService.Api.Services;
using Shared.Contracts;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Common;
using Shared.Infrastructure.Validation;

namespace ContentService.Api.Endpoints;

public static class MaterialEndpoints
{
    public static IEndpointRouteBuilder MapMaterialEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/content/materials").WithTags("Materials").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal principal, string? chapter, IMaterialService service, CancellationToken ct) =>
        {
            var includeInactive = IsTeacherOrAdmin(principal);
            var result = await service.ListAsync(includeInactive, chapter, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<MaterialResponse>>.Ok(result));
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IMaterialService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, IsTeacherOrAdmin(principal), ct);
            return Results.Ok(ApiResponse<MaterialResponse>.Ok(result));
        });

        group.MapPost("/", async (CreateMaterialRequest request, ClaimsPrincipal principal, IMaterialService service, CancellationToken ct) =>
            {
                var result = await service.CreateAsync(request, principal.GetUserId(), ct);
                return Results.Created($"/api/v1/content/materials/{result.Id}", ApiResponse<MaterialResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<CreateMaterialRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Roles.Teacher, Roles.Admin));

        group.MapPut("/{id:guid}", async (Guid id, UpdateMaterialRequest request, IMaterialService service, CancellationToken ct) =>
            {
                var result = await service.UpdateAsync(id, request, ct);
                return Results.Ok(ApiResponse<MaterialResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<UpdateMaterialRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Roles.Teacher, Roles.Admin));

        group.MapDelete("/{id:guid}", async (Guid id, IMaterialService service, CancellationToken ct) =>
            {
                await service.DeleteAsync(id, ct);
                return Results.Ok(ApiResponse.Ok());
            })
            .RequireAuthorization(policy => policy.RequireRole(Roles.Teacher, Roles.Admin));

        group.MapPost("/{id:guid}/view", async (Guid id, IMaterialService service, CancellationToken ct) =>
        {
            var viewCount = await service.IncrementViewCountAsync(id, ct);
            return Results.Ok(ApiResponse<int>.Ok(viewCount));
        });

        return app;
    }

    private static bool IsTeacherOrAdmin(ClaimsPrincipal principal) =>
        principal.IsInRole(Roles.Teacher) || principal.IsInRole(Roles.Admin);
}
