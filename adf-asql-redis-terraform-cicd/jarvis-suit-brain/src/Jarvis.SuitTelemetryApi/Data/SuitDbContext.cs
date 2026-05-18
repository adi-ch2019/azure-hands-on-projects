using Microsoft.EntityFrameworkCore;
using Jarvis.Shared;

namespace Jarvis.SuitTelemetryApi.Data
{
    public class SuitDbContext : DbContext
    {
        public SuitDbContext(DbContextOptions<SuitDbContext> options) : base(options) { }
        
        public DbSet<SuitStatusEvent> SuitTelemetry { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SuitStatusEvent>()
                .HasKey(e => e.EventId);
            
            modelBuilder.Entity<SuitStatusEvent>()
                .HasIndex(e => e.SuitId)
                .HasDatabaseName("IX_SuitId");
            
            modelBuilder.Entity<SuitStatusEvent>()
                .HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_Timestamp");
        }
    }
}