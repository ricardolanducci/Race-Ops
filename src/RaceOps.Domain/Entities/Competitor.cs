namespace RaceOps.Domain.Entities;

public class Competitor
{
    public string RacerId { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Nationality { get; init; } = string.Empty;
    public string ClassId { get; init; } = string.Empty;
    public string Position { get; init; } = string.Empty;
    public string Laps { get; init; } = string.Empty;
    public string TotalTime { get; init; } = string.Empty;
    public string BestLap { get; init; } = string.Empty;
    public string BestLapTime { get; init; } = string.Empty;
    public string LastLapTime { get; init; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}".Trim();
}
