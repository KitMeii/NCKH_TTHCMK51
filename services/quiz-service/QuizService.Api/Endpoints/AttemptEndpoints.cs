using System.Security.Claims;
using QuizService.Api.Dtos;
using QuizService.Api.Services;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Common;
using Shared.Infrastructure.Validation;

namespace QuizService.Api.Endpoints;

public static class AttemptEndpoints
{
    public static IEndpointRouteBuilder MapAttemptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/quiz").WithTags("Attempts").RequireAuthorization();

        group.MapPost("/practice/submit", async (SubmitQuizRequest request, ClaimsPrincipal principal, IQuizAttemptService service, CancellationToken ct) =>
            {
                var result = await service.SubmitPracticeAsync(principal.GetUserId(), request, ct);
                return Results.Ok(ApiResponse<SubmitResultResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<SubmitQuizRequest>>();

        group.MapPost("/exams/submit", async (SubmitExamRequest request, ClaimsPrincipal principal, IQuizAttemptService service, CancellationToken ct) =>
            {
                var result = await service.SubmitExamAsync(principal.GetUserId(), request, ct);
                return Results.Ok(ApiResponse<SubmitResultResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<SubmitExamRequest>>();

        group.MapGet("/wrong-answers", async (ClaimsPrincipal principal, IQuizAttemptService service, CancellationToken ct) =>
        {
            var result = await service.GetWrongAnswersAsync(principal.GetUserId(), ct);
            return Results.Ok(ApiResponse<IReadOnlyList<WrongAnswerResponse>>.Ok(result));
        });

        group.MapPost("/oral/submit", async (SubmitOralRequest request, ClaimsPrincipal principal, IOralAttemptService service, CancellationToken ct) =>
            {
                var result = await service.SubmitAsync(principal.GetUserId(), request, ct);
                return Results.Ok(ApiResponse<OralResultResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<SubmitOralRequest>>();

        group.MapGet("/oral/results", async (ClaimsPrincipal principal, IOralAttemptService service, CancellationToken ct) =>
        {
            var result = await service.GetMyResultsAsync(principal.GetUserId(), ct);
            return Results.Ok(ApiResponse<IReadOnlyList<OralResultResponse>>.Ok(result));
        });

        return app;
    }
}
