using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Common;

namespace AdminService.Api.Clients;

/// <summary>Calls auth-service's admin-only user endpoints. Those endpoints also require the
/// X-Internal-Key header (see RequireInternalServiceKeyFilter) — this is the only place that
/// header should ever be sent from, since admin-service is their one legitimate caller.</summary>
public sealed class HttpAuthAdminClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    IOptions<InternalServiceAuthOptions> internalServiceAuthOptions) : IAuthAdminClient
{
    public async Task<IReadOnlyList<RemoteUser>> ListUsersAsync(string? role, CancellationToken ct)
    {
        var url = string.IsNullOrWhiteSpace(role) ? "/api/v1/auth/users" : $"/api/v1/auth/users?role={role}";
        using var message = ForwardedRequest(HttpMethod.Get, url);

        var response = await httpClient.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<RemoteUser>>>(cancellationToken: ct);
        return body?.Data ?? [];
    }

    public async Task<RemoteUser> ChangeRoleAsync(Guid userId, string newRole, CancellationToken ct)
    {
        using var message = ForwardedRequest(HttpMethod.Put, $"/api/v1/auth/users/{userId}/role");
        message.Content = JsonContent.Create(new { Role = newRole });

        var response = await httpClient.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RemoteUser>>(cancellationToken: ct)
            ?? throw new InvalidOperationException("auth-service returned an empty role-change response.");

        return body.Data ?? throw new InvalidOperationException("auth-service returned a successful response with no user data.");
    }

    private HttpRequestMessage ForwardedRequest(HttpMethod method, string url)
    {
        var message = new HttpRequestMessage(method, url);
        var incomingAuth = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(incomingAuth) && AuthenticationHeaderValue.TryParse(incomingAuth, out var parsed))
        {
            message.Headers.Authorization = parsed;
        }

        message.Headers.Add(RequireInternalServiceKeyFilter.HeaderName, internalServiceAuthOptions.Value.SharedKey);

        return message;
    }
}
