using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuizService.Api.Data;
using QuizService.Api.Dtos;
using QuizService.Api.Entities;
using QuizService.Api.Progress;
using Shared.Infrastructure.Common;

namespace QuizService.Api.Services;

/// <summary>
/// Server-side grading. This is the fix for audit finding F3: the old frontend fetched the full
/// answer key to the browser, graded itself, and just wrote whatever score it computed straight
/// to the database — a student could fabricate a perfect score from devtools without answering
/// anything. Here the client sends only its selected options; the correct answers never leave
/// this service until after grading, and the score is always computed from the stored Question
/// rows, never trusted from the request.
/// </summary>
public sealed class QuizAttemptService(QuizDbContext db, IProgressReporter progressReporter, ILogger<QuizAttemptService> logger) : IQuizAttemptService
{
    public async Task<SubmitResultResponse> SubmitPracticeAsync(Guid userId, SubmitQuizRequest request, CancellationToken ct)
    {
        var (result, gradedAnswers) = await GradeAsync(request.Answers, ct);

        db.QuizResults.Add(new QuizResult
        {
            UserId = userId,
            Chapter = request.Chapter,
            Score = result.Score,
            Correct = result.Correct,
            Total = result.Total,
        });

        await RecordWrongAnswersAsync(userId, gradedAnswers, ct);
        await db.SaveChangesAsync(ct);
        await ReportScoreBestEffortAsync(userId, result.Score, ct);

        return result;
    }

    public async Task<SubmitResultResponse> SubmitExamAsync(Guid userId, SubmitExamRequest request, CancellationToken ct)
    {
        var (result, gradedAnswers) = await GradeAsync(request.Answers, ct);

        db.ExamResults.Add(new ExamResult
        {
            UserId = userId,
            Score = result.Score,
            Correct = result.Correct,
            Total = result.Total,
            TimeSpentSeconds = request.TimeSpentSeconds,
        });

        await RecordWrongAnswersAsync(userId, gradedAnswers, ct);
        await db.SaveChangesAsync(ct);
        await ReportScoreBestEffortAsync(userId, result.Score, ct);

        return result;
    }

    private async Task ReportScoreBestEffortAsync(Guid userId, decimal score, CancellationToken ct)
    {
        try
        {
            await progressReporter.ReportScoreAsync(score, ct);
        }
        catch (Exception ex)
        {
            // progress-service tracks the leaderboard/average — secondary to the grading result
            // itself, so a temporary outage there must not fail the student's quiz submission.
            logger.LogWarning(ex, "Failed to report score to progress-service for user {UserId}", userId);
        }
    }

    public async Task<IReadOnlyList<WrongAnswerResponse>> GetWrongAnswersAsync(Guid userId, CancellationToken ct)
    {
        var rows = await (
            from wrong in db.WrongAnswers
            join question in db.Questions on wrong.QuestionId equals question.Id
            where wrong.UserId == userId
            orderby wrong.WrongCount descending, wrong.LastWrongAtUtc descending
            select new WrongAnswerResponse(question.Id, question.QuestionText, question.Chapter, wrong.WrongCount, wrong.LastWrongAtUtc)
        ).ToListAsync(ct);

        return rows;
    }

    public async Task<IReadOnlyList<MyResultResponse>> GetMyResultsAsync(Guid userId, CancellationToken ct)
    {
        var quizResults = await db.QuizResults.Where(r => r.UserId == userId)
            .Select(r => new MyResultResponse(r.Id, "practice", r.Chapter, r.Score, r.Correct, r.Total, r.CreatedAtUtc))
            .ToListAsync(ct);

        var examResults = await db.ExamResults.Where(r => r.UserId == userId)
            .Select(r => new MyResultResponse(r.Id, "exam", null, r.Score, r.Correct, r.Total, r.CreatedAtUtc))
            .ToListAsync(ct);

        return quizResults.Concat(examResults).OrderByDescending(r => r.CreatedAtUtc).ToList();
    }

    private async Task<(SubmitResultResponse Result, IReadOnlyList<GradedAnswer> GradedAnswers)> GradeAsync(
        IReadOnlyList<SubmitAnswerItem> answers, CancellationToken ct)
    {
        var questionIds = answers.Select(a => a.QuestionId).Distinct().ToList();
        var questions = await db.Questions.Where(q => questionIds.Contains(q.Id)).ToDictionaryAsync(q => q.Id, ct);

        var missing = questionIds.Where(id => !questions.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            throw new NotFoundException($"Không tìm thấy {missing.Count} câu hỏi trong bài nộp.");
        }

        var graded = new List<GradedAnswer>(answers.Count);
        var correctCount = 0;

        foreach (var answer in answers)
        {
            var question = questions[answer.QuestionId];
            var isCorrect = answer.SelectedOption == question.CorrectAnswer;
            if (isCorrect)
            {
                correctCount++;
            }

            graded.Add(new GradedAnswer(question.Id, answer.SelectedOption, question.CorrectAnswer, isCorrect, question.Explanation));
        }

        var total = answers.Count;
        var score = total > 0 ? Math.Round(correctCount * 10m / total, 2) : 0m;

        return (new SubmitResultResponse(score, correctCount, total, graded), graded);
    }

    private async Task RecordWrongAnswersAsync(Guid userId, IReadOnlyList<GradedAnswer> gradedAnswers, CancellationToken ct)
    {
        var wrongQuestionIds = gradedAnswers.Where(a => !a.IsCorrect).Select(a => a.QuestionId).ToList();
        if (wrongQuestionIds.Count == 0)
        {
            return;
        }

        var existing = await db.WrongAnswers
            .Where(w => w.UserId == userId && wrongQuestionIds.Contains(w.QuestionId))
            .ToDictionaryAsync(w => w.QuestionId, ct);

        foreach (var questionId in wrongQuestionIds)
        {
            if (existing.TryGetValue(questionId, out var wrongAnswer))
            {
                wrongAnswer.WrongCount++;
                wrongAnswer.LastWrongAtUtc = DateTime.UtcNow;
            }
            else
            {
                db.WrongAnswers.Add(new WrongAnswer { UserId = userId, QuestionId = questionId, WrongCount = 1, LastWrongAtUtc = DateTime.UtcNow });
            }
        }
    }
}
