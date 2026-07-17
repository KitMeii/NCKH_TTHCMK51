using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuthService.Api.Dtos;
using Microsoft.Extensions.Options;
using Shared.Contracts;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Common;
using Xunit;

namespace AuthService.Tests.Integration;

public sealed class AdminUserManagementTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client;

    public AdminUserManagementTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static HttpRequestMessage WithAuth(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    // GET /users and PUT /users/{id}/role additionally require X-Internal-Key — simulates the
    // one legitimate caller (admin-service's HttpAuthAdminClient). See RequireInternalServiceKeyFilter.
    private static HttpRequestMessage WithAuthAndInternalKey(HttpMethod method, string url, string token)
    {
        var request = WithAuth(method, url, token);
        request.Headers.Add("X-Internal-Key", AuthApiFactory.TestInternalServiceKey);
        return request;
    }

    // Must match appsettings.Development.json, which the test WebApplicationFactory loads.
    private static readonly JwtTokenService TestTokenService = new(Options.Create(new JwtOptions
    {
        Issuer = "tthcm-platform",
        Audience = "tthcm-services",
        SigningKey = "dev-only-signing-key-do-not-use-in-production-min-32-chars",
    }));

    private static string AdminToken(out Guid adminId)
    {
        adminId = Guid.NewGuid();
        return TestTokenService.IssueAccessToken(adminId.ToString(), "admin@test.local", "Admin Test", Roles.Admin).AccessToken;
    }

    [Fact]
    public async Task Student_cannot_list_users()
    {
        var email = $"student-list-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "P@ssw0rd123", "Student"));
        var studentToken = (await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!.AccessToken;

        var request = WithAuth(HttpMethod.Get, "/api/v1/auth/users", studentToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_change_a_students_role_and_it_persists()
    {
        var email = $"promote-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "P@ssw0rd123", "Học viên X"));
        var student = (await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        var adminToken = AdminToken(out _);

        var changeRoleRequest = WithAuthAndInternalKey(HttpMethod.Put, $"/api/v1/auth/users/{student.User.Id}/role", adminToken);
        changeRoleRequest.Content = JsonContent.Create(new ChangeRoleRequest(Roles.Teacher));
        var response = await _client.SendAsync(changeRoleRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        Assert.Equal(Roles.Teacher, body!.Data!.Role);

        // Confirm it actually persisted, not just echoed back in the response.
        var listRequest = WithAuthAndInternalKey(HttpMethod.Get, "/api/v1/auth/users?role=Teacher", adminToken);
        var listResponse = await _client.SendAsync(listRequest);
        var list = (await listResponse.Content.ReadFromJsonAsync<ApiResponse<List<UserResponse>>>())!.Data!;
        Assert.Contains(list, u => u.Id == student.User.Id);
    }

    [Fact]
    public async Task Admin_cannot_change_own_role()
    {
        var adminToken = AdminToken(out var adminId);

        var request = WithAuthAndInternalKey(HttpMethod.Put, $"/api/v1/auth/users/{adminId}/role", adminToken);
        request.Content = JsonContent.Create(new ChangeRoleRequest(Roles.Student));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Change_role_rejects_unknown_role_value()
    {
        var email = $"badrole-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "P@ssw0rd123", "Học viên Y"));
        var student = (await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        var request = WithAuthAndInternalKey(HttpMethod.Put, $"/api/v1/auth/users/{student.User.Id}/role", AdminToken(out _));
        request.Content = JsonContent.Create(new ChangeRoleRequest("SuperAdmin"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Phần A RBAC audit: these two admin-only endpoints must not be callable with just a valid
    // Admin JWT — that would let any client bypass admin-service's audit log by hitting
    // auth-service directly through the gateway's /api/v1/auth/** catch-all route. Only a caller
    // that also knows InternalService:SharedKey (i.e. admin-service) may proceed.
    [Fact]
    public async Task Admin_without_internal_key_cannot_change_role()
    {
        var email = $"bypass-attempt-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "P@ssw0rd123", "Học viên Z"));
        var student = (await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        var request = WithAuth(HttpMethod.Put, $"/api/v1/auth/users/{student.User.Id}/role", AdminToken(out _));
        request.Content = JsonContent.Create(new ChangeRoleRequest(Roles.Teacher));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_without_internal_key_cannot_list_users()
    {
        var request = WithAuth(HttpMethod.Get, "/api/v1/auth/users", AdminToken(out _));
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
