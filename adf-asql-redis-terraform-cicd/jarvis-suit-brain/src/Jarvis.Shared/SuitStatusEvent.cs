using System;

namespace Jarvis.Shared
{
    public class SuitStatusEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public string SuitId { get; set; } = string.Empty;
        public string SuitModel { get; set; } = string.Empty;  // Mark85, Mark42, etc.
        public int PowerLevel { get; set; }  // 0-100
        public string Status { get; set; } = string.Empty;  // "Active", "Damaged", "Repairing"
        public string Location { get; set; } = string.Empty;  // "AvengersTower", "Malibu", etc.
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // For battle coordination
        public bool IsInBattle { get; set; }
        public string ThreatLevel { get; set; } = "Low";  // "Low", "Medium", "High", "Thanos"
    }
}