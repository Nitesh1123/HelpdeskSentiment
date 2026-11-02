using HelpdeskSentiment.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpdeskSentiment.Data
{
    // Code First: the database schema is generated from these entity classes by EF Core migrations.
    public class HelpdeskDbContext : DbContext
    {
        public HelpdeskDbContext(DbContextOptions<HelpdeskDbContext> options) : base(options)
        {
        }

        public DbSet<CallRecord> CallRecords => Set<CallRecord>();
        public DbSet<Agent> Agents => Set<Agent>();
        public DbSet<LoginActivity> LoginActivities => Set<LoginActivity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Agent>(e =>
            {
                e.HasIndex(a => a.Email).IsUnique();
            });

            modelBuilder.Entity<CallRecord>(e =>
            {
                e.Property(c => c.Category).HasConversion<string>().HasMaxLength(20);
                e.Property(c => c.Sentiment).HasConversion<string>().HasMaxLength(10);
                e.Property(c => c.PhoneNumber).HasMaxLength(10);
                // [Required] is only for form validation; in the DB the agent can be cleared when the agent is deleted.
                e.Property(c => c.AgentId).IsRequired(false);
                e.HasIndex(c => c.Sentiment);
                e.HasOne(c => c.Agent)
                 .WithMany(a => a.Calls)
                 .HasForeignKey(c => c.AgentId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<LoginActivity>(e =>
            {
                e.ToTable("LoginActivity");
                e.Property(l => l.Event).HasConversion<string>().HasMaxLength(20);
                e.HasIndex(l => l.OccurredAt);
            });

            modelBuilder.Entity<Agent>().HasData(
                new Agent { Id = 1, Name = "Riya Sharma", Email = "riya@helpdesk.local" },
                new Agent { Id = 2, Name = "Aman Verma", Email = "aman@helpdesk.local" },
                new Agent { Id = 3, Name = "Kavya Singh", Email = "kavya@helpdesk.local" });
        }
    }
}
