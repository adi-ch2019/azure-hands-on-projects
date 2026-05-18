using Jarvis.SuitWorker.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Add JARVIS coordinator as background service
builder.Services.AddHostedService<JarvisCoordinator>();

// Add health checks
builder.Services.AddHealthChecks();

var host = builder.Build();

// Log startup
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🦾 JARVIS Suit Worker starting up...");

host.Run();