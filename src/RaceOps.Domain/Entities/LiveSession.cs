namespace RaceOps.Domain.Entities;

public class LiveSession
{
    public string RunNumber { get; init; } = string.Empty;
    public string SessionName { get; init; } = string.Empty;
    public string TrackName { get; init; } = string.Empty;
    public string TrackLength { get; init; } = string.Empty;
    public string CurrentTime { get; init; } = string.Empty;
    public string SessionTime { get; init; } = string.Empty;
    public string TimeToGo { get; init; } = string.Empty;
    public string LapsToGo { get; init; } = string.Empty;
    public FlagStatus FlagStatus { get; init; }
    public string SortMode { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, RaceClass> Classes { get; init; } = new Dictionary<string, RaceClass>();
    public IReadOnlyDictionary<string, Competitor> Competitors { get; init; } = new Dictionary<string, Competitor>();
}
