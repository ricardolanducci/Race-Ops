namespace RaceOps.Infrastructure.Database.Models;

public class RaceCacheModel
{
    public int RaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Track { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
}
