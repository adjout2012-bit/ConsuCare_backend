using ConsuCare.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsuCare.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<SupporterProfile> SupporterProfiles => Set<SupporterProfile>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();
    public DbSet<SupportConnection> SupportConnections => Set<SupportConnection>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Goal>()
            .HasMany(g => g.Milestones)
            .WithOne()
            .HasForeignKey(m => m.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
    }
}
