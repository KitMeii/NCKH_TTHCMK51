using Microsoft.Extensions.Options;
using Shared.Contracts;
using Shared.Infrastructure.Auth;

namespace ProgressService.Tests.Integration;

public static class TestTokens
{
    public static string Student(Guid userId) =>
        new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "tthcm-platform",
            Audience = "tthcm-services",
            SigningKey = "dev-only-signing-key-do-not-use-in-production-min-32-chars",
        })).IssueAccessToken(userId.ToString(), "student@test.local", "Student Test", Roles.Student).AccessToken;
}
