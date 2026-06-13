using RaceOps.Domain.Entities;

namespace RaceOps.Domain.Interfaces;

public interface IRaceMonitorClient
{
    Task<IReadOnlyList<Race>> GetCurrentRacesAsync(CancellationToken ct = default);
    Task<LiveSession> GetSessionAsync(int raceId, CancellationToken ct = default);
    Task<CompetitorDetail> GetRacerAsync(int raceId, string racerId, CancellationToken ct = default);
    Task<StreamingConnectionInfo> GetStreamingConnectionAsync(int raceId, CancellationToken ct = default);
}
