using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Data;

/// <summary>
/// Applies pending EF Core migrations with a short retry loop. Needed because in docker-compose,
/// the auth-service container can start before Postgres has finished accepting connections even
/// when Postgres's own healthcheck says "starting" — a bare `db.Database.Migrate()` on first
/// boot would crash the whole process on that transient race instead of just waiting it out.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task MigrateWithRetryAsync(
        AuthDbContext db,
        ILogger logger,
        int maxAttempts = 10,
        TimeSpan? delay = null)
    {
        delay ??= TimeSpan.FromSeconds(3);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Database not ready yet, retrying migration ({Attempt}/{MaxAttempts})...", attempt, maxAttempts);
                await Task.Delay(delay.Value);
            }
        }
    }
}
