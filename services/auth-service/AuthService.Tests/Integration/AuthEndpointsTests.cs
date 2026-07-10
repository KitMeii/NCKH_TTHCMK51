using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuthService.Api.Dtos;
using Shared.Contracts;
using Shared.Infrastructure.Common;
using Xunit;

namespace AuthService.Tests.Integration;

public sealed class AuthEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_then_login_then_me_returns_correct_role_claim()
    {
        var email = $"student-{Guid.NewGuid():N}@test.local";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "P@ssw0rd123", "Nguyễn Văn A"));

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(registerBody);
        Assert.True(registerBody!.Success);
        Assert.Equal(Roles.Student, registerBody.Data!.User.Role);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "P@ssw0rd123"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(loginBody);
        var accessToken = loginBody!.Data!.AccessToken;

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var meResponse = await _client.SendAsync(meRequest);

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var meBody = await meResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        Assert.NotNull(meBody);
        Assert.Equal(email, meBody!.Data!.Email);
        Assert.Equal(Roles.Student, meBody.Data.Role);
    }

    [Fact]
    public async Task Me_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_with_duplicate_email_returns_conflict()
    {
        var email = $"dup-{Guid.NewGuid():N}@test.local";
        var request = new RegisterRequest(email, "P@ssw0rd123", "Trần Thị B");

        var first = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var body = await second.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.False(body!.Success);
        Assert.Equal(ErrorCodes.Conflict, body.Error!.Code);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401_with_standard_envelope()
    {
        var email = $"wrongpw-{Guid.NewGuid():N}@test.local";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "P@ssw0rd123", "Lê Văn C"));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.False(body!.Success);
        Assert.Equal(ErrorCodes.Unauthorized, body.Error!.Code);
    }

    [Fact]
    public async Task Register_with_weak_password_returns_validation_error()
    {
        var email = $"weak-{Guid.NewGuid():N}@test.local";
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "short", "Phạm Thị D"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.False(body!.Success);
        Assert.Equal(ErrorCodes.ValidationError, body.Error!.Code);
    }
}
