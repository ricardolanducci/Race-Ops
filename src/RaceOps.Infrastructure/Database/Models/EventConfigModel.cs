namespace RaceOps.Infrastructure.Database.Models;

public class EventConfigModel
{
    public int RaceId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int RaceDurationMinutes { get; set; }

    /// <summary>JSON: lista de PitRule (Name, MinStopSeconds, RequiredCount).</summary>
    public string PitRulesJson { get; set; } = "[]";

    /// <summary>JSON: lista de números de kart monitorados.</summary>
    public string MonitoredKartsJson { get; set; } = "[]";

    public bool HasHeavyStintRule { get; set; }
    public int HeavyStintRequiredCount { get; set; }
    public int TrackTimeAlertMinutes { get; set; }
    public double AttentionLapSeconds { get; set; }
    public double BoxLapSeconds { get; set; }
}
