using System.Text.Json;
using System.Text.Json.Serialization;
using AiService.Api.Caching;
using AiService.Api.Dtos;
using AiService.Api.Groq;

namespace AiService.Api.Services;

/// <summary>
/// Generates candidate MCQ questions from a teacher-uploaded document. This never writes to
/// quiz-service's question bank itself — the teacher reviews the generated questions in the UI
/// first, then quiz-service's own POST /api/v1/quiz/questions (Teacher/Admin only) is what
/// actually persists the ones they accept.
/// </summary>
public sealed class QuestionExtractionService(IGroqClient groqClient, ResponseCache cache) : IQuestionExtractionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ExtractQuestionsResponse> ExtractAsync(ExtractQuestionsRequest request, CancellationToken ct) =>
        cache.GetOrCreateAsync("extract-questions", $"{request.Chapter}|{request.Count}|{request.SourceText}", async () =>
        {
            var prompt =
                $"Từ nội dung tài liệu chương \"{request.Chapter}\" sau đây, hãy soạn {request.Count} câu " +
                "hỏi trắc nghiệm 4 lựa chọn (A/B/C/D) bằng tiếng Việt, kèm đáp án đúng và giải thích ngắn " +
                "gọn. Trả lời CHỈ bằng một JSON array, không thêm văn bản nào khác, đúng định dạng: " +
                "[{\"question\": \"...\", \"optionA\": \"...\", \"optionB\": \"...\", \"optionC\": \"...\", " +
                "\"optionD\": \"...\", \"correctAnswer\": <0=A,1=B,2=C,3=D>, \"explanation\": \"...\"}, ...]" +
                "\n\nNội dung tài liệu:\n" + request.SourceText;

            var raw = await groqClient.CompleteAsync([new GroqMessage("user", prompt)], maxTokens: 4000, ct);
            var payload = ParsePayload(raw);

            var questions = payload.Select(q => new ExtractedQuestion(
                q.Question, q.OptionA, q.OptionB, q.OptionC, q.OptionD, q.CorrectAnswer, q.Explanation)).ToList();

            return new ExtractQuestionsResponse(questions);
        });

    private static List<QuestionPayload> ParsePayload(string raw)
    {
        var jsonText = MarkdownJson.StripCodeFence(raw.Trim());

        try
        {
            return JsonSerializer.Deserialize<List<QuestionPayload>>(jsonText, JsonOptions)
                ?? throw new InvalidOperationException("AI question-extraction response deserialized to null.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"AI question-extraction response was not valid JSON: {raw}", ex);
        }
    }

    private sealed record QuestionPayload(
        [property: JsonPropertyName("question")] string Question,
        [property: JsonPropertyName("optionA")] string OptionA,
        [property: JsonPropertyName("optionB")] string OptionB,
        [property: JsonPropertyName("optionC")] string OptionC,
        [property: JsonPropertyName("optionD")] string OptionD,
        [property: JsonPropertyName("correctAnswer")] int CorrectAnswer,
        [property: JsonPropertyName("explanation")] string? Explanation);
}
