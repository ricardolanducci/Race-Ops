namespace RaceOps.Domain.Entities;

public class Race
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Track { get; init; } = string.Empty;
    public bool IsLive { get; init; }
    public long StartDateEpoch { get; init; }
    public long EndDateEpoch { get; init; }
    public string TimeZoneId { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
}
