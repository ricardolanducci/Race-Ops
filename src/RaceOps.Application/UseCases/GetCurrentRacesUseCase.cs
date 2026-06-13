using RaceOps.Application.DTOs;
using RaceOps.Domain.Interfaces;

namespace RaceOps.Application.UseCases;

public class GetCurrentRacesUseCase(IRaceMonitorClient client)
{
    public async Task<IReadOnlyList<RaceDto>> ExecuteAsync(CancellationToken ct = default)
    {
        var races = await client.GetCurrentRacesAsync(ct);

        return races
            .Select(r => new RaceDto(r.Id, r.Name, r.Track, r.IsLive, r.StartDateEpoch, r.ImageUrl))
            .ToList();
    }
}
