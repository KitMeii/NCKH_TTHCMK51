using Microsoft.EntityFrameworkCore;
using QuizService.Api.Entities;

namespace QuizService.Api.Data;

public sealed class QuizDbContext(DbContextOptions<QuizDbContext> options) : DbContext(options)
{
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<OralQuestion> OralQuestions => Set<OralQuestion>();
    public DbSet<OralResult> OralResults => Set<OralResult>();
    public DbSet<ExamResult> ExamResults => Set<ExamResult>();
    public DbSet<QuizResult> QuizResults => Set<QuizResult>();
    public DbSet<WrongAnswer> WrongAnswers => Set<WrongAnswer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("quiz");

        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("questions");
            entity.HasKey(q => q.Id);
            entity.Property(q => q.Chapter).HasMaxLength(128);
            entity.HasIndex(q => q.Chapter);
        });

        modelBuilder.Entity<OralQuestion>(entity =>
        {
            entity.ToTable("oral_questions");
            entity.HasKey(q => q.Id);
            entity.Property(q => q.Chapter).HasMaxLength(128);
            entity.HasIndex(q => q.Chapter);
        });

        modelBuilder.Entity<OralResult>(entity =>
        {
            entity.ToTable("oral_results");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.AiScore).HasColumnType("decimal(4,2)");
            entity.HasIndex(r => r.UserId);
            entity.HasOne<OralQuestion>().WithMany().HasForeignKey(r => r.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExamResult>(entity =>
        {
            entity.ToTable("exam_results");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Score).HasColumnType("decimal(4,2)");
            entity.HasIndex(r => r.UserId);
        });

        modelBuilder.Entity<QuizResult>(entity =>
        {
            entity.ToTable("quiz_results");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Score).HasColumnType("decimal(4,2)");
            entity.Property(r => r.Chapter).HasMaxLength(128);
            entity.HasIndex(r => r.UserId);
        });

        modelBuilder.Entity<WrongAnswer>(entity =>
        {
            entity.ToTable("wrong_answers");
            entity.HasKey(w => new { w.UserId, w.QuestionId });
            entity.HasOne<Question>().WithMany().HasForeignKey(w => w.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
