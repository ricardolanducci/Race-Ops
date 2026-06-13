using RaceOps.Domain.Entities;
using RaceOps.Domain.Interfaces;

namespace RaceOps.Application.UseCases;

public class GetStreamingConnectionUseCase(IRaceMonitorClient client)
{
    public Task<StreamingConnectionInfo> ExecuteAsync(int raceId, CancellationToken ct = default)
        => client.GetStreamingConnectionAsync(raceId, ct);
}
