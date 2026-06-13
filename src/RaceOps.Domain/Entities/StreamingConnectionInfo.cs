namespace RaceOps.Domain.Entities;

public class StreamingConnectionInfo
{
    public int RaceId { get; init; }
    public string WebsocketUrl { get; init; } = string.Empty;
    public string LiveTimingToken { get; init; } = string.Empty;
    public int Instance { get; init; }
    public bool IsLive { get; init; }
}
