using Microsoft.EntityFrameworkCore;
using RaceOps.Infrastructure.Database.Models;

namespace RaceOps.Infrastructure.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<RaceCacheModel> RaceCache => Set<RaceCacheModel>();
    public DbSet<UserPreferencesModel> UserPreferences => Set<UserPreferencesModel>();
    public DbSet<EventConfigModel> EventConfigs => Set<EventConfigModel>();
    public DbSet<StintModel> Stints => Set<StintModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RaceCacheModel>().HasKey(x => x.RaceId);
        modelBuilder.Entity<EventConfigModel>().HasKey(x => x.RaceId);
        modelBuilder.Entity<StintModel>()
            .HasIndex(x => new { x.RaceId, x.KartNumber, x.Number })
            .IsUnique();
        modelBuilder.Entity<UserPreferencesModel>().HasData(
            new UserPreferencesModel { Id = 1, SelectedRaceId = null, Layout = "{}" }
        );
    }
}
