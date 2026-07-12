namespace QuizService.Api.Dtos;

public sealed record SubmitAnswerItem(Guid QuestionId, int SelectedOption);

public sealed record SubmitQuizRequest(string? Chapter, List<SubmitAnswerItem> Answers);

public sealed record SubmitExamRequest(List<SubmitAnswerItem> Answers, int TimeSpentSeconds);

/// <summary>Per-question grading detail returned only AFTER submission — this is the one place
/// CorrectAnswer is allowed to reach the client, since by then the student has already answered.</summary>
public sealed record GradedAnswer(Guid QuestionId, int SelectedOption, int CorrectAnswer, bool IsCorrect, string? Explanation);

public sealed record SubmitResultResponse(decimal Score, int Correct, int Total, IReadOnlyList<GradedAnswer> Details);

public sealed record WrongAnswerResponse(Guid QuestionId, string QuestionText, string? Chapter, int WrongCount, DateTime LastWrongAtUtc);

/// <summary>One row of a student's own history — practice or exam, distinguished by Kind.
/// Feeds tien-do.html's per-chapter breakdown and progress-service's leaderboard aggregate.</summary>
public sealed record MyResultResponse(Guid Id, string Kind, string? Chapter, decimal Score, int Correct, int Total, DateTime CreatedAtUtc);

public sealed record SubmitOralRequest(Guid QuestionId, string MainAnswer, List<string>? FollowupAnswers);

public sealed record OralResultResponse(Guid Id, Guid QuestionId, string MainAnswer, decimal AiScore, string? AiComment, IReadOnlyDictionary<string, decimal>? RubricScores, DateTime CreatedAtUtc);
