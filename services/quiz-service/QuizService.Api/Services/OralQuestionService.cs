using Microsoft.EntityFrameworkCore;
using QuizService.Api.Data;
using QuizService.Api.Dtos;
using QuizService.Api.Entities;
using Shared.Infrastructure.Common;

namespace QuizService.Api.Services;

public sealed class OralQuestionService(QuizDbContext db) : IOralQuestionService
{
    public async Task<IReadOnlyList<OralQuestionResponse>> ListAsync(string? chapter, CancellationToken ct)
    {
        var query = db.OralQuestions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(chapter))
        {
            query = query.Where(q => q.Chapter == chapter);
        }

        var questions = await query.OrderBy(q => q.Chapter).ThenByDescending(q => q.CreatedAtUtc).ToListAsync(ct);
        return questions.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<OralQuestionPracticeResponse>> ListForPracticeAsync(string? chapter, CancellationToken ct)
    {
        var query = db.OralQuestions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(chapter))
        {
            query = query.Where(q => q.Chapter == chapter);
        }

        return await query
            .OrderBy(q => q.CreatedAtUtc)
            .Select(q => new OralQuestionPracticeResponse(q.Id, q.Chapter, q.QuestionText, q.Difficulty))
            .ToListAsync(ct);
    }

    public async Task<OralQuestionResponse> CreateAsync(CreateOralQuestionRequest request, Guid createdBy, CancellationToken ct)
    {
        var question = new OralQuestion
        {
            Chapter = request.Chapter?.Trim(),
            QuestionText = request.QuestionText.Trim(),
            ExpectedAnswer = request.ExpectedAnswer?.Trim(),
            Difficulty = request.Difficulty,
            CreatedBy = createdBy,
        };

        db.OralQuestions.Add(question);
        await db.SaveChangesAsync(ct);
        return ToResponse(question);
    }

    public async Task<OralQuestionResponse> UpdateAsync(Guid id, UpdateOralQuestionRequest request, CancellationToken ct)
    {
        var question = await db.OralQuestions.FindAsync([id], ct)
            ?? throw new NotFoundException("Không tìm thấy câu hỏi vấn đáp.");

        question.Chapter = request.Chapter?.Trim();
        question.QuestionText = request.QuestionText.Trim();
        question.ExpectedAnswer = request.ExpectedAnswer?.Trim();
        question.Difficulty = request.Difficulty;

        await db.SaveChangesAsync(ct);
        return ToResponse(question);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var question = await db.OralQuestions.FindAsync([id], ct)
            ?? throw new NotFoundException("Không tìm thấy câu hỏi vấn đáp.");

        db.OralQuestions.Remove(question);
        await db.SaveChangesAsync(ct);
    }

    private static OralQuestionResponse ToResponse(OralQuestion q) =>
        new(q.Id, q.Chapter, q.QuestionText, q.ExpectedAnswer, q.Difficulty, q.CreatedBy, q.CreatedAtUtc);
}
