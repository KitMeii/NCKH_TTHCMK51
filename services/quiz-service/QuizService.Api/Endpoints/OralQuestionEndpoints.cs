using System.Security.Claims;
using QuizService.Api.Dtos;
using QuizService.Api.Services;
using Shared.Contracts;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Common;
using Shared.Infrastructure.Validation;

namespace QuizService.Api.Endpoints;

public static class OralQuestionEndpoints
{
    public static IEndpointRouteBuilder MapOralQuestionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/quiz/oral-questions").WithTags("OralQuestions").RequireAuthorization();

        group.MapGet("/", async (string? chapter, IOralQuestionService service, CancellationToken ct) =>
            {
                var result = await service.ListAsync(chapter, ct);
                return Results.Ok(ApiResponse<IReadOnlyList<OralQuestionResponse>>.Ok(result));
            })
            .RequireAuthorization(policy => policy.RequireRole(Roles.Teacher, Roles.Admin));

        group.MapGet("/practice", async (string? chapter, IOralQuestionService service, CancellationToken ct) =>
        {
            var result = await service.ListForPracticeAsync(chapter, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<OralQuestionPracticeResponse>>.Ok(result));
        });

        group.MapPost("/", async (CreateOralQuestionRequest request, ClaimsPrincipal principal, IOralQuestionService service, CancellationToken ct) =>
            {
                var result = await service.CreateAsync(request, principal.GetUserId(), ct);
                return Results.Created($"/api/v1/quiz/oral-questions/{result.Id}", ApiResponse<OralQuestionResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<CreateOralQuestionRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Roles.Teacher, Roles.Admin));

        group.MapPut("/{id:guid}", async (Guid id, UpdateOralQuestionRequest request, IOralQuestionService service, CancellationToken ct) =>
            {
                var result = await service.UpdateAsync(id, request, ct);
                return Results.Ok(ApiResponse<OralQuestionResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<UpdateOralQuestionRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Roles.Teacher, Roles.Admin));

        group.MapDelete("/{id:guid}", async (Guid id, IOralQuestionService service, CancellationToken ct) =>
            {
                await service.DeleteAsync(id, ct);
                return Results.Ok(ApiResponse.Ok());
            })
            .RequireAuthorization(policy => policy.RequireRole(Roles.Teacher, Roles.Admin));

        return app;
    }
}
