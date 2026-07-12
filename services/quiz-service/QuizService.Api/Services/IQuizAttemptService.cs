using QuizService.Api.Dtos;

namespace QuizService.Api.Services;

public interface IQuizAttemptService
{
    Task<SubmitResultResponse> SubmitPracticeAsync(Guid userId, SubmitQuizRequest request, CancellationToken ct);
    Task<SubmitResultResponse> SubmitExamAsync(Guid userId, SubmitExamRequest request, CancellationToken ct);
    Task<IReadOnlyList<WrongAnswerResponse>> GetWrongAnswersAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<MyResultResponse>> GetMyResultsAsync(Guid userId, CancellationToken ct);
}
