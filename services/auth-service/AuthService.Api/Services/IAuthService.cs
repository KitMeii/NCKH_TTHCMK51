using AuthService.Api.Dtos;

namespace AuthService.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<UserResponse> GetByIdAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<UserNameResponse>> GetNamesByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct);
    Task<IReadOnlyList<UserResponse>> ListUsersAsync(string? role, CancellationToken ct);
    Task<UserResponse> ChangeRoleAsync(Guid userId, string newRole, CancellationToken ct);
    Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct);
}
