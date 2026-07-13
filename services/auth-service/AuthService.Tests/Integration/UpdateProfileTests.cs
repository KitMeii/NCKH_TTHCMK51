using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuthService.Api.Dtos;
using Shared.Infrastructure.Common;
using Xunit;

namespace AuthService.Tests.Integration;

public sealed class UpdateProfileTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client;

    public UpdateProfileTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Update_profile_persists_name_course_and_class()
    {
        var email = $"profile-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "P@ssw0rd123", "Tên Cũ"));
        var auth = (await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        using var updateRequest = new HttpRequestMessage(HttpMethod.Put, "/api/v1/auth/me")
        {
            Content = JsonContent.Create(new UpdateProfileRequest("Tên Mới", "K51", "CNTT1")),
        };
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var updateResponse = await _client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>())!.Data!;
        Assert.Equal("Tên Mới", updated.Name);
        Assert.Equal("K51", updated.Course);
        Assert.Equal("CNTT1", updated.ClassName);

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var meResponse = await _client.SendAsync(meRequest);
        var me = (await meResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>())!.Data!;

        Assert.Equal("Tên Mới", me.Name);
        Assert.Equal("CNTT1", me.ClassName);
    }
}
