namespace QuizService.Api.Dtos;

public sealed record CreateOralQuestionRequest(string? Chapter, string QuestionText, string? ExpectedAnswer, int Difficulty);

public sealed record UpdateOralQuestionRequest(string? Chapter, string QuestionText, string? ExpectedAnswer, int Difficulty);

/// <summary>Full detail including ExpectedAnswer — Teacher/Admin bank management only.</summary>
public sealed record OralQuestionResponse(Guid Id, string? Chapter, string QuestionText, string? ExpectedAnswer, int Difficulty, Guid? CreatedBy, DateTime CreatedAtUtc);

/// <summary>What a student sees before attempting — no ExpectedAnswer (that's only for AI grading).</summary>
public sealed record OralQuestionPracticeResponse(Guid Id, string? Chapter, string QuestionText, int Difficulty);
