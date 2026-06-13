using RaceOps.Application.DTOs;
using RaceOps.Domain.Interfaces;

namespace RaceOps.Application.UseCases;

public class GetLiveSessionUseCase(IRaceMonitorClient client)
{
    public async Task<LiveSessionDto> ExecuteAsync(int raceId, CancellationToken ct = default)
    {
        var session = await client.GetSessionAsync(raceId, ct);

        var classes = session.Classes.Values
            .Select(c => new ClassDto(c.ClassId, c.Description))
            .ToList();

        var competitors = session.Competitors.Values
            .OrderBy(c => int.TryParse(c.Position, out var pos) ? pos : int.MaxValue)
            .Select(c => new CompetitorDto(
                c.Position,
                c.Number,
                c.FullName,
                c.ClassId,
                c.Laps,
                c.BestLapTime,
                c.LastLapTime,
                c.TotalTime))
            .ToList();

        return new LiveSessionDto(
            session.SessionName,
            session.TrackName,
            session.FlagStatus.ToString(),
            session.TimeToGo,
            session.LapsToGo,
            session.SessionTime,
            session.SortMode,
            classes,
            competitors);
    }
}
