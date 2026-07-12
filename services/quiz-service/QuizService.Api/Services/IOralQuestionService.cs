using QuizService.Api.Dtos;

namespace QuizService.Api.Services;

public interface IOralQuestionService
{
    Task<IReadOnlyList<OralQuestionResponse>> ListAsync(string? chapter, CancellationToken ct);
    Task<IReadOnlyList<OralQuestionPracticeResponse>> ListForPracticeAsync(string? chapter, CancellationToken ct);
    Task<OralQuestionResponse> CreateAsync(CreateOralQuestionRequest request, Guid createdBy, CancellationToken ct);
    Task<OralQuestionResponse> UpdateAsync(Guid id, UpdateOralQuestionRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
