using System.Reflection;
using ContentService.Api.Data;
using ContentService.Api.Endpoints;
using ContentService.Api.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Data;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.HealthChecks;
using Shared.Infrastructure.Observability;
using Shared.Infrastructure.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedObservability("content-service");

var connectionString = builder.Configuration.GetConnectionString("ContentDb")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:ContentDb configuration.");

builder.Services.AddDbContext<ContentDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddSharedHealthChecksSqlServer(connectionString);

builder.Services.AddSharedJwtAuthentication(builder.Configuration);
builder.Services.AddSharedCors(builder.Configuration);
builder.Services.AddSharedValidation(Assembly.GetExecutingAssembly());

builder.Services.AddScoped<IMaterialService, MaterialService>();

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

app.MapMaterialEndpoints();
app.MapSharedHealthChecks();

if (builder.Configuration.GetValue("Database:AutoMigrate", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
    await DatabaseInitializer.MigrateWithRetryAsync(db, app.Logger);
}

app.Run();

public partial class Program;
