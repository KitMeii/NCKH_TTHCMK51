using Microsoft.EntityFrameworkCore;
using QuizService.Api.Data;
using QuizService.Api.Dtos;
using QuizService.Api.Entities;
using Shared.Infrastructure.Common;

namespace QuizService.Api.Services;

public sealed class QuestionService(QuizDbContext db) : IQuestionService
{
    public async Task<IReadOnlyList<QuestionResponse>> ListAsync(string? chapter, CancellationToken ct)
    {
        var query = db.Questions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(chapter))
        {
            query = query.Where(q => q.Chapter == chapter);
        }

        var questions = await query.OrderBy(q => q.Chapter).ThenByDescending(q => q.CreatedAtUtc).ToListAsync(ct);
        return questions.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<QuizQuestionResponse>> ListForPracticeAsync(string? chapter, CancellationToken ct)
    {
        var query = db.Questions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(chapter))
        {
            query = query.Where(q => q.Chapter == chapter);
        }

        return await query
            .OrderBy(q => q.CreatedAtUtc)
            .Select(q => new QuizQuestionResponse(q.Id, q.Chapter, q.QuestionText, q.OptionA, q.OptionB, q.OptionC, q.OptionD))
            .ToListAsync(ct);
    }

    public async Task<QuestionResponse> CreateAsync(CreateQuestionRequest request, Guid createdBy, CancellationToken ct)
    {
        var question = new Question
        {
            Chapter = request.Chapter?.Trim(),
            QuestionText = request.QuestionText.Trim(),
            OptionA = request.OptionA,
            OptionB = request.OptionB,
            OptionC = request.OptionC,
            OptionD = request.OptionD,
            CorrectAnswer = request.CorrectAnswer,
            Explanation = request.Explanation?.Trim(),
            CreatedBy = createdBy,
        };

        db.Questions.Add(question);
        await db.SaveChangesAsync(ct);
        return ToResponse(question);
    }

    public async Task<QuestionResponse> UpdateAsync(Guid id, UpdateQuestionRequest request, CancellationToken ct)
    {
        var question = await db.Questions.FindAsync([id], ct)
            ?? throw new NotFoundException("Không tìm thấy câu hỏi.");

        question.Chapter = request.Chapter?.Trim();
        question.QuestionText = request.QuestionText.Trim();
        question.OptionA = request.OptionA;
        question.OptionB = request.OptionB;
        question.OptionC = request.OptionC;
        question.OptionD = request.OptionD;
        question.CorrectAnswer = request.CorrectAnswer;
        question.Explanation = request.Explanation?.Trim();

        await db.SaveChangesAsync(ct);
        return ToResponse(question);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var question = await db.Questions.FindAsync([id], ct)
            ?? throw new NotFoundException("Không tìm thấy câu hỏi.");

        db.Questions.Remove(question);
        await db.SaveChangesAsync(ct);
    }

    private static QuestionResponse ToResponse(Question q) => new(
        q.Id, q.Chapter, q.QuestionText, q.OptionA, q.OptionB, q.OptionC, q.OptionD,
        q.CorrectAnswer, q.Explanation, q.CreatedBy, q.CreatedAtUtc);
}
