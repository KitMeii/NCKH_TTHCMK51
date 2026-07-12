using AiService.Api.Dtos;
using AiService.Api.Services;
using Shared.Contracts;
using Shared.Infrastructure.Common;
using Shared.Infrastructure.Validation;

namespace AiService.Api.Endpoints;

public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai").WithTags("Ai").RequireAuthorization();

        group.MapPost("/chat", async (ChatRequest request, IChatService service, CancellationToken ct) =>
            {
                var result = await service.ChatAsync(request, ct);
                return Results.Ok(ApiResponse<ChatResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<ChatRequest>>();

        group.MapPost("/generate-lecture", async (GenerateLectureRequest request, ILectureService service, CancellationToken ct) =>
            {
                var result = await service.GenerateLectureAsync(request, ct);
                return Results.Ok(ApiResponse<GenerateLectureResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<GenerateLectureRequest>>();

        group.MapPost("/generate-comprehension-questions", async (GenerateComprehensionQuestionsRequest request, ILectureService service, CancellationToken ct) =>
            {
                var result = await service.GenerateComprehensionQuestionsAsync(request, ct);
                return Results.Ok(ApiResponse<GenerateComprehensionQuestionsResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<GenerateComprehensionQuestionsRequest>>();

        // Called by quiz-service (service-to-service, not by the frontend directly) to grade a
        // vấn đáp answer — see QuizService.Api/Grading/IOralGradingClient.cs on the caller side.
        group.MapPost("/grade-oral", async (GradeOralRequest request, IOralGradingService service, CancellationToken ct) =>
            {
                var result = await service.GradeAsync(request, ct);
                return Results.Ok(ApiResponse<GradeOralResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<GradeOralRequest>>();

        group.MapPost("/extract-questions", async (ExtractQuestionsRequest request, IQuestionExtractionService service, CancellationToken ct) =>
            {
                var result = await service.ExtractAsync(request, ct);
                return Results.Ok(ApiResponse<ExtractQuestionsResponse>.Ok(result));
            })
            .AddEndpointFilter<ValidationEndpointFilter<ExtractQuestionsRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Roles.Teacher, Roles.Admin));

        return app;
    }
}
