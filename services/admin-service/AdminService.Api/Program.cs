using System.Reflection;
using AdminService.Api.Clients;
using AdminService.Api.Data;
using AdminService.Api.Endpoints;
using AdminService.Api.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Data;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.HealthChecks;
using Shared.Infrastructure.Observability;
using Shared.Infrastructure.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedObservability("admin-service");

var connectionString = builder.Configuration.GetConnectionString("AdminDb")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:AdminDb configuration.");

builder.Services.AddDbContext<AdminDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddSharedHealthChecksSqlServer(connectionString);

builder.Services.AddSharedJwtAuthentication(builder.Configuration);
builder.Services.AddSharedCors(builder.Configuration);
builder.Services.AddSharedValidation(Assembly.GetExecutingAssembly());
builder.Services.AddInternalServiceAuth(builder.Configuration);
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<ISystemConfigService, SystemConfigService>();
builder.Services.AddScoped<ISystemOverviewService, SystemOverviewService>();
builder.Services.AddScoped<ISystemStatsClient, HttpSystemStatsClient>();

var authServiceBaseUrl = builder.Configuration.GetValue("Services:Auth:BaseUrl", "http://auth-service:8080")!;
builder.Services.AddHttpClient<IAuthAdminClient, HttpAuthAdminClient>(client =>
{
    client.BaseAddress = new Uri(authServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient("content-service", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue("Services:Content:BaseUrl", "http://content-service:8080")!);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient("quiz-service", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue("Services:Quiz:BaseUrl", "http://quiz-service:8080")!);
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

app.MapAdminEndpoints();
app.MapSharedHealthChecks();

if (builder.Configuration.GetValue("Database:AutoMigrate", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    await DatabaseInitializer.MigrateWithRetryAsync(db, app.Logger);
}

app.Run();

public partial class Program;
