using Azure.Messaging.ServiceBus;
using System.Text.Json;
using Jarvis.Shared;

namespace Jarvis.SuitWorker.Workers;

public class JarvisCoordinator : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly ILogger<JarvisCoordinator> _logger;
    private readonly HttpClient _httpClient;

    public JarvisCoordinator(IConfiguration configuration, ILogger<JarvisCoordinator> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        
        var connectionString = configuration.GetConnectionString("ServiceBus")
            ?? throw new InvalidOperationException("Service Bus connection missing");
        
        var client = new ServiceBusClient(connectionString);
        _processor = client.CreateProcessor("suit-events", new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 5,
            AutoCompleteMessages = false  // We control completion
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor.ProcessMessageAsync += ProcessSuitEventAsync;
        _processor.ProcessErrorAsync += ErrorHandlerAsync;
        
        await _processor.StartProcessingAsync(stoppingToken);
        _logger.LogInformation("JARVIS Coordinator started - monitoring all suits");
        
        // Keep running until stopped
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessSuitEventAsync(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();
        var suitEvent = JsonSerializer.Deserialize<SuitStatusEvent>(body);
        
        if (suitEvent == null)
        {
            _logger.LogWarning("JARVIS received malformed event");
            await args.CompleteMessageAsync(args.Message);
            return;
        }
        
        _logger.LogInformation("JARVIS processing event for {SuitId}: Power={PowerLevel}, Status={Status}", 
            suitEvent.SuitId, suitEvent.PowerLevel, suitEvent.Status);
        
        // JARVIS intelligence - decision logic
        if (suitEvent.PowerLevel < 20)
        {
            _logger.LogWarning("⚠️ JARVIS: {SuitId} power critical! Alerting Tony", suitEvent.SuitId);
            await AlertTonyStark(suitEvent);
        }
        
        if (suitEvent.Status == "Damaged" && suitEvent.ThreatLevel == "High")
        {
            _logger.LogWarning("🛡️ JARVIS: {SuitId} damaged in battle! Dispatching backup", suitEvent.SuitId);
            await DispatchBackupSuit(suitEvent);
        }
        
        if (suitEvent.ThreatLevel == "Thanos")
        {
            _logger.LogCritical("💀 JARVIS: THANOS LEVEL THREAT - Activating all suits!");
            await ActivateAllSuits();
        }
        
        // Log to console for demo
        _logger.LogInformation("✅ JARVIS: {SuitId} status recorded at {Timestamp}", 
            suitEvent.SuitId, suitEvent.Timestamp);
        
        await args.CompleteMessageAsync(args.Message);
    }

    private Task AlertTonyStark(SuitStatusEvent suitEvent)
    {
        // In real life, this would send SMS/Teams/PageDuty
        _logger.LogCritical("🚨 ALERT TONY: {SuitId} needs assistance!", suitEvent.SuitId);
        return Task.CompletedTask;
    }

    private Task DispatchBackupSuit(SuitStatusEvent suitEvent)
    {
        _logger.LogInformation("🦾 Dispatching House Party Protocol for {SuitId}", suitEvent.SuitId);
        return Task.CompletedTask;
    }

    private Task ActivateAllSuits()
    {
        _logger.LogInformation("🔴 AVENGERS ASSEMBLE! Activating all suits globally");
        return Task.CompletedTask;
    }

    private Task ErrorHandlerAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "JARVIS encountered an error in message processing");
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        await _processor.StopProcessingAsync(stoppingToken);
        await _processor.DisposeAsync();
        _httpClient.Dispose();
        await base.StopAsync(stoppingToken);
    }
}