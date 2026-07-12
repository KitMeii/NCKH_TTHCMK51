using QuizService.Api.Dtos;

namespace QuizService.Api.Services;

public interface IQuestionService
{
    Task<IReadOnlyList<QuestionResponse>> ListAsync(string? chapter, CancellationToken ct);
    Task<IReadOnlyList<QuizQuestionResponse>> ListForPracticeAsync(string? chapter, CancellationToken ct);
    Task<QuestionResponse> CreateAsync(CreateQuestionRequest request, Guid createdBy, CancellationToken ct);
    Task<QuestionResponse> UpdateAsync(Guid id, UpdateQuestionRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
