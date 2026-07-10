using AuthService.Api.Dtos;

namespace AuthService.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<UserResponse> GetByIdAsync(Guid userId, CancellationToken ct);
}
