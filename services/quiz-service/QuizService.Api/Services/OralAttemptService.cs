using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuizService.Api.Data;
using QuizService.Api.Dtos;
using QuizService.Api.Entities;
using QuizService.Api.Grading;
using Shared.Infrastructure.Common;

namespace QuizService.Api.Services;

public sealed class OralAttemptService(QuizDbContext db, IOralGradingClient gradingClient) : IOralAttemptService
{
    public async Task<OralResultResponse> SubmitAsync(Guid userId, SubmitOralRequest request, CancellationToken ct)
    {
        var question = await db.OralQuestions.FindAsync([request.QuestionId], ct)
            ?? throw new NotFoundException("Không tìm thấy câu hỏi vấn đáp.");

        var followups = request.FollowupAnswers ?? [];
        var grading = await gradingClient.GradeAsync(
            new OralGradingRequest(question.QuestionText, question.ExpectedAnswer, request.MainAnswer, followups), ct);

        var result = new OralResult
        {
            UserId = userId,
            QuestionId = question.Id,
            MainAnswer = request.MainAnswer,
            FollowupAnswersJson = followups.Count > 0 ? JsonSerializer.Serialize(followups) : null,
            AiScore = grading.Score,
            AiComment = grading.Comment,
            RubricScoresJson = grading.RubricScores is { Count: > 0 } ? JsonSerializer.Serialize(grading.RubricScores) : null,
        };

        db.OralResults.Add(result);
        await db.SaveChangesAsync(ct);

        return ToResponse(result);
    }

    public async Task<IReadOnlyList<OralResultResponse>> GetMyResultsAsync(Guid userId, CancellationToken ct)
    {
        var results = await db.OralResults
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(ct);

        return results.Select(ToResponse).ToList();
    }

    private static OralResultResponse ToResponse(OralResult r) => new(
        r.Id,
        r.QuestionId,
        r.MainAnswer,
        r.AiScore,
        r.AiComment,
        r.RubricScoresJson is null ? null : JsonSerializer.Deserialize<Dictionary<string, decimal>>(r.RubricScoresJson),
        r.CreatedAtUtc);
}
