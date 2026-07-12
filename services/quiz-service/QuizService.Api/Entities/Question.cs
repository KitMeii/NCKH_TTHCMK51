namespace QuizService.Api.Entities;

public sealed class Question
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string? Chapter { get; set; }
    public required string QuestionText { get; set; }
    public required string OptionA { get; set; }
    public required string OptionB { get; set; }
    public required string OptionC { get; set; }
    public required string OptionD { get; set; }

    /// <summary>0=A, 1=B, 2=C, 3=D. Never serialized to a student-facing response before grading.</summary>
    public required int CorrectAnswer { get; set; }

    public string? Explanation { get; set; }
    public Guid? CreatedBy { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
