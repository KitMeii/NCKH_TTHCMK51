using AuthService.Api.Data;
using AuthService.Api.Dtos;
using AuthService.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Common;

namespace AuthService.Api.Services;

public sealed class AuthServiceImpl(AuthDbContext db, IJwtTokenService tokenService) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailTaken = await db.Users.AnyAsync(u => u.Email == normalizedEmail, ct);
        if (emailTaken)
        {
            throw new ConflictException("Email đã được đăng ký.");
        }

        // Self-registration is always Student — Teacher/Admin accounts are only created by
        // admin-service, closing the F1 privilege-escalation hole from the old Supabase RLS design.
        var user = new User
        {
            Email = normalizedEmail,
            Name = request.Name.Trim(),
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = Roles.Student,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthenticationFailedException("Email hoặc mật khẩu không đúng.");
        }

        return BuildAuthResponse(user);
    }

    public async Task<UserResponse> GetByIdAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("Không tìm thấy người dùng.");

        return ToUserResponse(user);
    }

    public async Task<IReadOnlyList<UserNameResponse>> GetNamesByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        return await db.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UserNameResponse(u.Id, u.Name))
            .ToListAsync(ct);
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var token = tokenService.IssueAccessToken(user.Id.ToString(), user.Email, user.Name, user.Role);
        return new AuthResponse(token.AccessToken, token.ExpiresAtUtc, ToUserResponse(user));
    }

    private static UserResponse ToUserResponse(User user) => new(user.Id, user.Email, user.Name, user.Role);
}
