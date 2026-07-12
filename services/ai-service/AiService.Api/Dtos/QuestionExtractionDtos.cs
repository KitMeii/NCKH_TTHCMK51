namespace AiService.Api.Dtos;

public sealed record ExtractQuestionsRequest(string Chapter, string SourceText, int Count = 10);

public sealed record ExtractedQuestion(string QuestionText, string OptionA, string OptionB, string OptionC, string OptionD, int CorrectAnswer, string? Explanation);

public sealed record ExtractQuestionsResponse(List<ExtractedQuestion> Questions);
