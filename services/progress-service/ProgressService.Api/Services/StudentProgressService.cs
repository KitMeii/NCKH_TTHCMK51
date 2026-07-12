using Microsoft.EntityFrameworkCore;
using ProgressService.Api.Clients;
using ProgressService.Api.Data;
using ProgressService.Api.Dtos;
using ProgressService.Api.Entities;

namespace ProgressService.Api.Services;

public sealed class StudentProgressService(ProgressDbContext db, IUserNameLookupClient nameLookup) : IStudentProgressService
{
    public async Task RecordScoreAsync(Guid userId, decimal score, CancellationToken ct)
    {
        var progress = await db.StudentProgress.FindAsync([userId], ct);
        if (progress is null)
        {
            progress = new StudentProgress { UserId = userId };
            db.StudentProgress.Add(progress);
        }

        progress.TotalAttempts++;
        progress.ScoreSum += score;
        progress.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task<MyProgressResponse> GetMyProgressAsync(Guid userId, CancellationToken ct)
    {
        var progress = await db.StudentProgress.FindAsync([userId], ct);
        if (progress is null)
        {
            return new MyProgressResponse(0, null, 0, 0, 0m);
        }

        return ToResponse(progress);
    }

    public async Task<IReadOnlyList<LeaderboardEntryResponse>> GetLeaderboardAsync(int top, CancellationToken ct)
    {
        var topProgress = await db.StudentProgress
            .Where(p => p.TotalAttempts > 0)
            .OrderByDescending(p => p.ScoreSum / p.TotalAttempts)
            .ThenByDescending(p => p.Streak)
            .Take(top)
            .ToListAsync(ct);

        var names = await nameLookup.GetNamesAsync(topProgress.Select(p => p.UserId).ToList(), ct);

        return topProgress
            .Select(p => new LeaderboardEntryResponse(
                p.UserId,
                names.GetValueOrDefault(p.UserId, "Học viên"),
                Math.Round(p.ScoreSum / p.TotalAttempts, 2),
                p.Streak,
                p.TotalAttempts))
            .ToList();
    }

    private static MyProgressResponse ToResponse(StudentProgress p) => new(
        p.Streak,
        p.LastStudyDate,
        p.TotalStudyMinutes,
        p.TotalAttempts,
        p.TotalAttempts > 0 ? Math.Round(p.ScoreSum / p.TotalAttempts, 2) : 0m);
}
