namespace RaceOps.Domain.Entities;

public class CompetitorDetail
{
    public Competitor Competitor { get; init; } = new();
    public IReadOnlyList<LapTime> Laps { get; init; } = [];
}

public class LapTime
{
    public string Lap { get; init; } = string.Empty;
    public string Position { get; init; } = string.Empty;
    public string Time { get; init; } = string.Empty;
    public string FlagStatus { get; init; } = string.Empty;
    public string TotalTime { get; init; } = string.Empty;
}
