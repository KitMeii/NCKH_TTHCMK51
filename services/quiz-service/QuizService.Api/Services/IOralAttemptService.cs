using QuizService.Api.Dtos;

namespace QuizService.Api.Services;

public interface IOralAttemptService
{
    Task<OralResultResponse> SubmitAsync(Guid userId, SubmitOralRequest request, CancellationToken ct);
    Task<IReadOnlyList<OralResultResponse>> GetMyResultsAsync(Guid userId, CancellationToken ct);
}
