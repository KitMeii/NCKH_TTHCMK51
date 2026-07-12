using AiService.Api.Caching;
using AiService.Api.Dtos;
using AiService.Api.Groq;

namespace AiService.Api.Services;

public sealed class LectureService(IGroqClient groqClient, ResponseCache cache) : ILectureService
{
    public Task<GenerateLectureResponse> GenerateLectureAsync(GenerateLectureRequest request, CancellationToken ct) =>
        cache.GetOrCreateAsync("lecture", $"{request.Chapter}|{request.Topic}|{request.SourceText}", async () =>
        {
            var prompt =
                $"Dựa trên nội dung sau của chương \"{request.Chapter}\" - chủ đề \"{request.Topic}\", " +
                "hãy viết một bài giảng dẫn dắt bằng tiếng Việt, giọng văn như giảng viên đang giảng bài " +
                "trực tiếp cho học viên, giải thích dễ hiểu, có ví dụ minh hoạ.\n\nNội dung tài liệu:\n" +
                request.SourceText;

            var content = await groqClient.CompleteAsync([new GroqMessage("user", prompt)], maxTokens: 4000, ct);
            return new GenerateLectureResponse(content);
        });

    public Task<GenerateComprehensionQuestionsResponse> GenerateComprehensionQuestionsAsync(GenerateComprehensionQuestionsRequest request, CancellationToken ct) =>
        cache.GetOrCreateAsync("comprehension", $"{request.Chapter}|{request.SourceText}", async () =>
        {
            var prompt =
                $"Dựa trên nội dung chương \"{request.Chapter}\" sau đây, hãy tạo 3 câu hỏi kiểm tra " +
                "mức độ hiểu bài ngắn gọn bằng tiếng Việt. Mỗi câu hỏi một dòng, không đánh số, không " +
                "giải thích thêm.\n\nNội dung:\n" + request.SourceText;

            var raw = await groqClient.CompleteAsync([new GroqMessage("user", prompt)], maxTokens: 300, ct);
            var questions = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => line.Length > 0)
                .ToList();

            return new GenerateComprehensionQuestionsResponse(questions);
        });
}
