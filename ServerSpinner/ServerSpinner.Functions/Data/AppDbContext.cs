using Microsoft.EntityFrameworkCore;
using ServerSpinner.Functions.Entities;

namespace ServerSpinner.Functions.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Streamer> Streamers => Set<Streamer>();
    public DbSet<StreamerSettings> StreamerSettings => Set<StreamerSettings>();
    public DbSet<SpinnerState> SpinnerStates => Set<SpinnerState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Streamer>()
            .HasIndex(s => s.TwitchUserId)
            .IsUnique();

        modelBuilder.Entity<Streamer>()
            .HasOne(s => s.Settings)
            .WithOne(s => s.Streamer)
            .HasForeignKey<StreamerSettings>(s => s.StreamerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Streamer>()
            .Property(s => s.DisplayName)
            .HasMaxLength(100);

        modelBuilder.Entity<StreamerSettings>()
            .Property(s => s.Theme)
            .HasMaxLength(50);

        modelBuilder.Entity<SpinnerState>()
            .HasKey(s => s.StreamerId);
    }
}