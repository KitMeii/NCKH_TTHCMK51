namespace QuizService.Api.Entities;

public sealed class OralResult
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid UserId { get; init; }
    public required Guid QuestionId { get; init; }
    public required string MainAnswer { get; set; }

    /// <summary>JSON-encoded string[] of follow-up answers.</summary>
    public string? FollowupAnswersJson { get; set; }

    public decimal AiScore { get; set; }
    public string? AiComment { get; set; }

    /// <summary>JSON-encoded Dictionary&lt;string, decimal&gt; rubric breakdown — kept as raw JSON
    /// so quiz-service doesn't need to know ai-service's exact rubric shape ahead of time.</summary>
    public string? RubricScoresJson { get; set; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
