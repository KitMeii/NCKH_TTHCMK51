namespace AiService.Api.Dtos;

public sealed record GenerateLectureRequest(string Chapter, string Topic, string SourceText);

public sealed record GenerateLectureResponse(string Content);

public sealed record GenerateComprehensionQuestionsRequest(string Chapter, string SourceText);

public sealed record GenerateComprehensionQuestionsResponse(List<string> Questions);
