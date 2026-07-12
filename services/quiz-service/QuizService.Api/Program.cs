using System.Reflection;
using Microsoft.EntityFrameworkCore;
using QuizService.Api.Data;
using QuizService.Api.Endpoints;
using QuizService.Api.Grading;
using QuizService.Api.Progress;
using QuizService.Api.Services;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Data;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.HealthChecks;
using Shared.Infrastructure.Observability;
using Shared.Infrastructure.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedObservability("quiz-service");

var connectionString = builder.Configuration.GetConnectionString("QuizDb")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:QuizDb configuration.");

builder.Services.AddDbContext<QuizDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSharedHealthChecks(connectionString);

builder.Services.AddSharedJwtAuthentication(builder.Configuration);
builder.Services.AddSharedCors(builder.Configuration);
builder.Services.AddSharedValidation(Assembly.GetExecutingAssembly());
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IOralQuestionService, OralQuestionService>();
builder.Services.AddScoped<IQuizAttemptService, QuizAttemptService>();
builder.Services.AddScoped<IOralAttemptService, OralAttemptService>();

var aiServiceBaseUrl = builder.Configuration.GetValue("Services:Ai:BaseUrl", "http://ai-service:8080")!;
builder.Services.AddHttpClient<IOralGradingClient, HttpOralGradingClient>(client =>
{
    client.BaseAddress = new Uri(aiServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var progressServiceBaseUrl = builder.Configuration.GetValue("Services:Progress:BaseUrl", "http://progress-service:8080")!;
builder.Services.AddHttpClient<IProgressReporter, HttpProgressReporter>(client =>
{
    client.BaseAddress = new Uri(progressServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSharedMiddleware();
app.UseAuthentication();
app.UseAuthorization();

app.MapQuestionEndpoints();
app.MapOralQuestionEndpoints();
app.MapAttemptEndpoints();
app.MapSharedHealthChecks();

if (builder.Configuration.GetValue("Database:AutoMigrate", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<QuizDbContext>();
    await DatabaseInitializer.MigrateWithRetryAsync(db, app.Logger);
}

app.Run();

public partial class Program;
