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
            entity.Property(q => q.QuestionText).HasMaxLength(2000);
            entity.Property(q => q.OptionA).HasMaxLength(500);
            entity.Property(q => q.OptionB).HasMaxLength(500);
            entity.Property(q => q.OptionC).HasMaxLength(500);
            entity.Property(q => q.OptionD).HasMaxLength(500);
            entity.Property(q => q.Explanation).HasMaxLength(2000);
            entity.HasIndex(q => q.Chapter);
        });

        modelBuilder.Entity<OralQuestion>(entity =>
        {
            entity.ToTable("oral_questions");
            entity.HasKey(q => q.Id);
            entity.Property(q => q.Chapter).HasMaxLength(128);
            entity.Property(q => q.QuestionText).HasMaxLength(2000);
            entity.Property(q => q.ExpectedAnswer).HasMaxLength(4000);
            entity.HasIndex(q => q.Chapter);
        });

        modelBuilder.Entity<OralResult>(entity =>
        {
            entity.ToTable("oral_results");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.MainAnswer).HasMaxLength(4000);
            entity.Property(r => r.AiScore).HasColumnType("decimal(4,2)");
            entity.HasIndex(r => r.UserId);
            // Restrict, not Cascade: an OralResult is a student's graded record (score, AI
            // comment, rubric — see F5 remediation). Deleting a question bank entry must not
            // silently wipe that history; OralQuestionService.DeleteAsync checks for this first
            // and returns a clean 409 instead of letting the DB throw an FK violation (Phần B
            // CRUD audit finding).
            entity.HasOne<OralQuestion>().WithMany().HasForeignKey(r => r.QuestionId).OnDelete(DeleteBehavior.Restrict);
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
