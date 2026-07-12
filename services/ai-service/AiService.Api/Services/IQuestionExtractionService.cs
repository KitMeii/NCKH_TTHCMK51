using AiService.Api.Dtos;

namespace AiService.Api.Services;

public interface IQuestionExtractionService
{
    Task<ExtractQuestionsResponse> ExtractAsync(ExtractQuestionsRequest request, CancellationToken ct);
}
