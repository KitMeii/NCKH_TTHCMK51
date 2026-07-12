using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using QuizService.Api.Dtos;
using Shared.Infrastructure.Common;
using Xunit;

namespace QuizService.Tests.Integration;

public sealed class GradingTests : IClassFixture<QuizApiFactory>
{
    private readonly HttpClient _client;

    public GradingTests(QuizApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static HttpRequestMessage WithAuth(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<QuestionResponse> CreateQuestionAsync(string questionText, int correctAnswer)
    {
        var request = WithAuth(HttpMethod.Post, "/api/v1/quiz/questions", TestTokens.Teacher());
        request.Content = JsonContent.Create(new CreateQuestionRequest("1", questionText, "A", "B", "C", "D", correctAnswer, "giải thích"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<QuestionResponse>>();
        return body!.Data!;
    }

    [Fact]
    public async Task Student_cannot_create_question()
    {
        var request = WithAuth(HttpMethod.Post, "/api/v1/quiz/questions", TestTokens.Student());
        request.Content = JsonContent.Create(new CreateQuestionRequest("1", "Q?", "A", "B", "C", "D", 0, null));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Practice_questions_endpoint_never_exposes_the_answer_key()
    {
        await CreateQuestionAsync("Câu hỏi bí mật?", correctAnswer: 2);

        var request = WithAuth(HttpMethod.Get, "/api/v1/quiz/questions/practice", TestTokens.Student());
        var response = await _client.SendAsync(request);
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("correctAnswer", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("explanation", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Practice_submit_grades_from_stored_answer_key_not_client_input()
    {
        var q1 = await CreateQuestionAsync("Q1?", correctAnswer: 1);
        var q2 = await CreateQuestionAsync("Q2?", correctAnswer: 3);

        // Client answers Q1 correctly and Q2 incorrectly. There is no "score" field the client
        // could send to fake a result — the request only carries the selected options.
        var request = WithAuth(HttpMethod.Post, "/api/v1/quiz/practice/submit", TestTokens.Student());
        request.Content = JsonContent.Create(new SubmitQuizRequest("1", [
            new SubmitAnswerItem(q1.Id, 1),
            new SubmitAnswerItem(q2.Id, 0),
        ]));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SubmitResultResponse>>();
        Assert.Equal(1, body!.Data!.Correct);
        Assert.Equal(2, body.Data.Total);
        Assert.Equal(5.0m, body.Data.Score);

        var q2Detail = body.Data.Details.Single(d => d.QuestionId == q2.Id);
        Assert.False(q2Detail.IsCorrect);
        Assert.Equal(3, q2Detail.CorrectAnswer);
    }

    [Fact]
    public async Task Wrong_answer_is_tracked_and_wrong_count_increments_on_repeat_mistake()
    {
        var question = await CreateQuestionAsync("Câu sai lặp lại?", correctAnswer: 0);
        var studentToken = TestTokens.Student();

        for (var i = 0; i < 2; i++)
        {
            var submitRequest = WithAuth(HttpMethod.Post, "/api/v1/quiz/practice/submit", studentToken);
            submitRequest.Content = JsonContent.Create(new SubmitQuizRequest("1", [new SubmitAnswerItem(question.Id, 3)]));
            await _client.SendAsync(submitRequest);
        }

        var wrongAnswersRequest = WithAuth(HttpMethod.Get, "/api/v1/quiz/wrong-answers", studentToken);
        var wrongAnswersResponse = await _client.SendAsync(wrongAnswersRequest);
        var wrongAnswers = await wrongAnswersResponse.Content.ReadFromJsonAsync<ApiResponse<List<WrongAnswerResponse>>>();

        var entry = Assert.Single(wrongAnswers!.Data!, w => w.QuestionId == question.Id);
        Assert.Equal(2, entry.WrongCount);
    }

    [Fact]
    public async Task Submit_with_unknown_question_id_returns_404()
    {
        var request = WithAuth(HttpMethod.Post, "/api/v1/quiz/practice/submit", TestTokens.Student());
        request.Content = JsonContent.Create(new SubmitQuizRequest("1", [new SubmitAnswerItem(Guid.NewGuid(), 0)]));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Exam_submit_persists_time_spent_and_computes_score()
    {
        var question = await CreateQuestionAsync("Câu thi thử?", correctAnswer: 2);

        var request = WithAuth(HttpMethod.Post, "/api/v1/quiz/exams/submit", TestTokens.Student());
        request.Content = JsonContent.Create(new SubmitExamRequest([new SubmitAnswerItem(question.Id, 2)], TimeSpentSeconds: 120));

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SubmitResultResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(10.0m, body!.Data!.Score);
        Assert.Equal(1, body.Data.Correct);
    }

    [Fact]
    public async Task Oral_submit_delegates_scoring_to_ai_service_and_never_trusts_a_client_supplied_score()
    {
        var createRequest = WithAuth(HttpMethod.Post, "/api/v1/quiz/oral-questions", TestTokens.Teacher());
        createRequest.Content = JsonContent.Create(new CreateOralQuestionRequest("2", "Trình bày tư tưởng...", "đáp án mẫu", 2));
        var createResponse = await _client.SendAsync(createRequest);
        var oralQuestion = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<OralQuestionResponse>>())!.Data!;

        var submitRequest = WithAuth(HttpMethod.Post, "/api/v1/quiz/oral/submit", TestTokens.Student());
        submitRequest.Content = JsonContent.Create(new SubmitOralRequest(oralQuestion.Id, "câu trả lời của học viên", ["ý bổ sung"]));
        var submitResponse = await _client.SendAsync(submitRequest);

        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        var result = (await submitResponse.Content.ReadFromJsonAsync<ApiResponse<OralResultResponse>>())!.Data!;
        Assert.Equal(FakeOralGradingClient.FixedScore, result.AiScore);
        Assert.NotNull(result.RubricScores);
    }

    [Fact]
    public async Task Unauthenticated_submit_returns_401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/quiz/practice/submit", new SubmitQuizRequest(null, [new SubmitAnswerItem(Guid.NewGuid(), 0)]));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
