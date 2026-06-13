namespace RaceOps.Infrastructure.Database.Models;

public class StintModel
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public string KartNumber { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Driver { get; set; } = string.Empty;
    public DateTimeOffset? EntryTime { get; set; }
    public int? EntryLap { get; set; }
    public DateTimeOffset? ExitTime { get; set; }
    public int? ExitLap { get; set; }
    public double? StopTimeSeconds { get; set; }
    public bool Heavy { get; set; }
    public bool IsManual { get; set; }
}
