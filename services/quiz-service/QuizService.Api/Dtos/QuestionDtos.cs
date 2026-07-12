namespace QuizService.Api.Dtos;

public sealed record CreateQuestionRequest(string? Chapter, string QuestionText, string OptionA, string OptionB, string OptionC, string OptionD, int CorrectAnswer, string? Explanation);

public sealed record UpdateQuestionRequest(string? Chapter, string QuestionText, string OptionA, string OptionB, string OptionC, string OptionD, int CorrectAnswer, string? Explanation);

/// <summary>Full question detail, including the answer key — Teacher/Admin bank management only.</summary>
public sealed record QuestionResponse(Guid Id, string? Chapter, string QuestionText, string OptionA, string OptionB, string OptionC, string OptionD, int CorrectAnswer, string? Explanation, Guid? CreatedBy, DateTime CreatedAtUtc);

/// <summary>What a student sees while attempting a quiz — no CorrectAnswer, no Explanation.</summary>
public sealed record QuizQuestionResponse(Guid Id, string? Chapter, string QuestionText, string OptionA, string OptionB, string OptionC, string OptionD);
