using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Azure.Messaging.ServiceBus;
using System.Text.Json;
using Jarvis.Shared;
using Jarvis.SuitTelemetryApi.Data;

namespace Jarvis.SuitTelemetryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuitController : ControllerBase
{
    private readonly SuitDbContext _db;
    private readonly IDistributedCache _cache;
    private readonly ServiceBusSender _serviceBusSender;
    private readonly ILogger<SuitController> _logger;

    public SuitController(
        SuitDbContext db,
        IDistributedCache cache,
        ServiceBusClient serviceBusClient,
        ILogger<SuitController> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
        _serviceBusSender = serviceBusClient.CreateSender("suit-events");
    }

    // POST /api/suit/status - Update suit status (JARVIS calls this constantly)
    [HttpPost("status")]
    public async Task<IActionResult> UpdateSuitStatus([FromBody] SuitStatusEvent suitEvent)
    {
        try
        {
            // 1. Store in cache for fast access (Redis)
            var cacheKey = $"suit:{suitEvent.SuitId}:status";
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(suitEvent), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            // 2. Store in database for history (SQL)
            await _db.SuitTelemetry.AddAsync(suitEvent);
            await _db.SaveChangesAsync();

            // 3. Send to Service Bus for async processing (JARVIS coordination)
            var message = new ServiceBusMessage(JsonSerializer.Serialize(suitEvent))
            {
                MessageId = suitEvent.EventId,
                Subject = "SuitStatusUpdated"
            };
            await _serviceBusSender.SendMessageAsync(message);

            _logger.LogInformation("JARVIS recorded status for suit {SuitId}: {Status}", suitEvent.SuitId, suitEvent.Status);
            
            return Ok(new { message = $"JARVIS recorded status for {suitEvent.SuitId}", eventId = suitEvent.EventId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JARVIS failed to process suit status");
            return StatusCode(500, "JARVIS encountered an error");
        }
    }

    // GET /api/suit/{suitId}/status - Get current suit status (fast from cache)
    [HttpGet("{suitId}/status")]
    public async Task<IActionResult> GetSuitStatus(string suitId)
    {
        var cacheKey = $"suit:{suitId}:status";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            return Ok(JsonSerializer.Deserialize<SuitStatusEvent>(cached));
        }
        
        // Fallback to database
        var latest = await _db.SuitTelemetry
            .Where(s => s.SuitId == suitId)
            .OrderByDescending(s => s.Timestamp)
            .FirstOrDefaultAsync();
            
        if (latest == null)
            return NotFound($"No data found for suit {suitId}");
            
        return Ok(latest);
    }

    // GET /api/suit/all - Get all suits (for Avengers display)
    [HttpGet("all")]
    public async Task<IActionResult> GetAllSuits()
    {
        var suits = await _db.SuitTelemetry
            .GroupBy(s => s.SuitId)
            .Select(g => g.OrderByDescending(x => x.Timestamp).First())
            .Take(100)
            .ToListAsync();
            
        return Ok(suits);
    }
}