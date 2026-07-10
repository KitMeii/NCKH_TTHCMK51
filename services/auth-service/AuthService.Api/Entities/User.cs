using Shared.Contracts;

namespace AuthService.Api.Entities;

public sealed class User
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Email { get; init; }
    public required string Name { get; set; }
    public required string PasswordHash { get; set; }
    public string Role { get; set; } = Roles.Student;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
