namespace AuthService.Api.Dtos;

public sealed record RegisterRequest(string Email, string Password, string Name);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserResponse(Guid Id, string Email, string Name, string Role);

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserResponse User);

/// <summary>Minimal display info other services (progress-service leaderboard, admin-service
/// roster) enrich cross-service data with — name only, not email/role, to keep exposure minimal.</summary>
public sealed record UserNameResponse(Guid Id, string Name);
