using System.Reflection;
using AiService.Api.Caching;
using AiService.Api.Endpoints;
using AiService.Api.Groq;
using AiService.Api.Services;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.HealthChecks;
using Shared.Infrastructure.Observability;
using Shared.Infrastructure.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedObservability("ai-service");

builder.Services.AddSharedHealthChecks(connectionString: null);
builder.Services.AddSharedJwtAuthentication(builder.Configuration);
builder.Services.AddSharedCors(builder.Configuration);
builder.Services.AddSharedValidation(Assembly.GetExecutingAssembly());

builder.Services.Configure<GroqOptions>(builder.Configuration.GetSection(GroqOptions.SectionName));
builder.Services.AddHttpClient<IGroqClient, HttpGroqClient>();

var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "ai-service:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddSingleton<ResponseCache>();

builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ILectureService, LectureService>();
builder.Services.AddScoped<IOralGradingService, OralGradingService>();
builder.Services.AddScoped<IQuestionExtractionService, QuestionExtractionService>();

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

app.MapAiEndpoints();
app.MapSharedHealthChecks();

app.Run();

public partial class Program;
