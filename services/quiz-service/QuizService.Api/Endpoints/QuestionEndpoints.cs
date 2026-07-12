using System.Security.Claims;
using QuizService.Api.Dtos;
using QuizService.Api.Services;
using Shared.Contracts;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Common;
using Shared.Infrastructure.Validation;

namespace QuizService.Api.Endpoints;

public static class QuestionEndpoints
{
    public static IEndpointRouteBuilder MapQuestionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/quiz/questions").WithTags("Questions").RequireAuthorization();

        group.MapGet("/", async (string? chapter, IQuestionService service, CancellationToken ct) =>
            {
                var result = await service.ListAsync(chapter, ct);
                return Results.Ok(ApiResponse<IReadOnlyList<QuestionResponse>>.Ok(result));
            })
            .RequireAuthorization(policy => policy.RequireRole(Roles.Teacher, Roles.Admin));

        group.MapGet("/practice", async (string? chapter, IQuestionService service, CancellationToken ct) =>
        {
            var result = await service.ListForPracticeAsync(chapter, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<QuizQuestionResponse>>.Ok(result));
        });

        group.MapPost("/", async (CreateQuestionRequest request, ClaimsPrincipal principal, IQuestionService service, CancellationToken ct) =>
            {
                var result = await service.CreateAsync(request, principal.GetUserId(), ct);
                return Results.Created($"/api/v1/quiz/questions/{result.Id}", ApiResponse<QuestionResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<CreateQuestionRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Roles.Teacher, Roles.Admin));

        group.MapPut("/{id:guid}", async (Guid id, UpdateQuestionRequest request, IQuestionService service, CancellationToken ct) =>
            {
                var result = await service.UpdateAsync(id, request, ct);
                return Results.Ok(ApiResponse<QuestionResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<UpdateQuestionRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Roles.Teacher, Roles.Admin));

        group.MapDelete("/{id:guid}", async (Guid id, IQuestionService service, CancellationToken ct) =>
            {
                await service.DeleteAsync(id, ct);
                return Results.Ok(ApiResponse.Ok());
            })
            .RequireAuthorization(policy => policy.RequireRole(Roles.Teacher, Roles.Admin));

        return app;
    }
}
