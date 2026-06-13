using RaceOps.Application.DTOs;
using RaceOps.Domain.Interfaces;

namespace RaceOps.Application.UseCases;

public class GetRacerDetailUseCase(IRaceMonitorClient client)
{
    public async Task<CompetitorDetailDto> ExecuteAsync(int raceId, string racerId, CancellationToken ct = default)
    {
        var detail = await client.GetRacerAsync(raceId, racerId, ct);

        var competitorDto = new CompetitorDto(
            detail.Competitor.Position,
            detail.Competitor.Number,
            detail.Competitor.FullName,
            detail.Competitor.ClassId,
            detail.Competitor.Laps,
            detail.Competitor.BestLapTime,
            detail.Competitor.LastLapTime,
            detail.Competitor.TotalTime);

        var laps = detail.Laps
            .Select(l => new LapTimeDto(l.Lap, l.Position, l.Time, l.FlagStatus, l.TotalTime))
            .ToList();

        return new CompetitorDetailDto(competitorDto, laps);
    }
}
