using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AdminService.Api.Clients;
using AdminService.Api.Dtos;
using Shared.Contracts;
using Shared.Infrastructure.Common;
using Xunit;

namespace AdminService.Tests.Integration;

public sealed class AdminEndpointsTests : IClassFixture<AdminApiFactory>
{
    private readonly AdminApiFactory _factory;
    private readonly HttpClient _client;

    public AdminEndpointsTests(AdminApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static HttpRequestMessage WithAuth(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    [Fact]
    public async Task Student_cannot_access_any_admin_endpoint()
    {
        var response = await _client.SendAsync(WithAuth(HttpMethod.Get, "/api/v1/admin/users", TestTokens.Student()));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_request_returns_401()
    {
        var response = await _client.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Change_role_persists_and_is_recorded_in_the_audit_log()
    {
        var targetId = Guid.NewGuid();
        _factory.AuthClient.Users.Add(new RemoteUser(targetId, "hocvien@test.local", "Học viên A", Roles.Student));
        var adminToken = TestTokens.Admin();

        var changeRequest = WithAuth(HttpMethod.Put, $"/api/v1/admin/users/{targetId}/role", adminToken);
        changeRequest.Content = JsonContent.Create(new ChangeRoleRequest(Roles.Teacher));
        var changeResponse = await _client.SendAsync(changeRequest);

        Assert.Equal(HttpStatusCode.OK, changeResponse.StatusCode);
        var changed = (await changeResponse.Content.ReadFromJsonAsync<ApiResponse<UserSummaryResponse>>())!.Data!;
        Assert.Equal(Roles.Teacher, changed.Role);

        var auditRequest = WithAuth(HttpMethod.Get, "/api/v1/admin/audit-log", adminToken);
        var auditResponse = await _client.SendAsync(auditRequest);
        var audit = (await auditResponse.Content.ReadFromJsonAsync<ApiResponse<List<RoleChangeAuditResponse>>>())!.Data!;

        var entry = Assert.Single(audit, a => a.TargetUserId == targetId);
        Assert.Equal(Roles.Student, entry.OldRole);
        Assert.Equal(Roles.Teacher, entry.NewRole);
    }

    [Fact]
    public async Task Change_role_for_unknown_user_returns_404()
    {
        var request = WithAuth(HttpMethod.Put, $"/api/v1/admin/users/{Guid.NewGuid()}/role", TestTokens.Admin());
        request.Content = JsonContent.Create(new ChangeRoleRequest(Roles.Teacher));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task System_config_can_be_set_and_read_back()
    {
        var adminToken = TestTokens.Admin();

        var setRequest = WithAuth(HttpMethod.Put, "/api/v1/admin/config/registration_enabled", adminToken);
        setRequest.Content = JsonContent.Create(new SetConfigRequest("true"));
        await _client.SendAsync(setRequest);

        var getRequest = WithAuth(HttpMethod.Get, "/api/v1/admin/config", adminToken);
        var getResponse = await _client.SendAsync(getRequest);
        var configs = (await getResponse.Content.ReadFromJsonAsync<ApiResponse<List<SystemConfigResponse>>>())!.Data!;

        var entry = Assert.Single(configs, c => c.Key == "registration_enabled");
        Assert.Equal("true", entry.Value);
    }

    [Fact]
    public async Task Overview_combines_user_role_counts_with_content_and_quiz_stats()
    {
        _factory.AuthClient.Users.Clear();
        _factory.AuthClient.Users.Add(new RemoteUser(Guid.NewGuid(), "s1@test.local", "S1", Roles.Student));
        _factory.AuthClient.Users.Add(new RemoteUser(Guid.NewGuid(), "s2@test.local", "S2", Roles.Student));
        _factory.AuthClient.Users.Add(new RemoteUser(Guid.NewGuid(), "t1@test.local", "T1", Roles.Teacher));
        _factory.StatsClient.Overview = new SystemOverview(MaterialCount: 5, QuestionCount: 40, OralQuestionCount: 10);

        var response = await _client.SendAsync(WithAuth(HttpMethod.Get, "/api/v1/admin/stats/overview", TestTokens.Admin()));
        var overview = (await response.Content.ReadFromJsonAsync<ApiResponse<SystemOverviewResponse>>())!.Data!;

        Assert.Equal(2, overview.TotalStudents);
        Assert.Equal(1, overview.TotalTeachers);
        Assert.Equal(5, overview.TotalMaterials);
        Assert.Equal(40, overview.TotalQuestions);
        Assert.Equal(10, overview.TotalOralQuestions);
    }
}
