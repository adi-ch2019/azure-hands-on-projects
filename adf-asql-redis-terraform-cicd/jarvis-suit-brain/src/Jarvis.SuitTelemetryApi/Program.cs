using Microsoft.EntityFrameworkCore;
using Azure.Messaging.ServiceBus;
using Jarvis.SuitTelemetryApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. SQL Database
var sqlConnection = builder.Configuration.GetConnectionString("SuitDatabase") 
    ?? throw new InvalidOperationException("SQL connection string required");
builder.Services.AddDbContext<SuitDbContext>(options =>
    options.UseSqlServer(sqlConnection));

// 2. Redis Cache
var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Redis connection string required");
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "JarvisCache:";
});

// 3. Service Bus Client
var serviceBusConnection = builder.Configuration.GetConnectionString("ServiceBus")
    ?? throw new InvalidOperationException("Service Bus connection string required");
builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusConnection));

// 4. Simple health checks (no external packages needed)
builder.Services.AddHealthChecks()
    .AddCheck("API", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("JARVIS API is running"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AvengersTower", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Ensure database exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SuitDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AvengersTower");
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();