using RaceOps.Application.DTOs;
using RaceOps.Domain.Entities;
using RaceOps.Domain.Interfaces;

namespace RaceOps.Application.UseCases;

public class GetEventConfigUseCase(IEventConfigRepository repository)
{
    public async Task<EventConfigDto> ExecuteAsync(int raceId, CancellationToken ct = default)
    {
        var config = await repository.GetAsync(raceId, ct) ?? GetLiveViewUseCase.DefaultConfig(raceId);
        return ToDto(config);
    }

    public static EventConfigDto ToDto(EventConfig config) => new(
        config.RaceId,
        config.EventName,
        config.RaceDurationMinutes,
        config.PitRules.Select(p => new PitRuleDto(p.Name, p.MinStopSeconds, p.RequiredCount)).ToList(),
        config.MonitoredKarts,
        config.HasHeavyStintRule,
        config.HeavyStintRequiredCount,
        config.TrackTimeAlertMinutes,
        config.AttentionLapSeconds,
        config.BoxLapSeconds);
}

public class SaveEventConfigUseCase(IEventConfigRepository repository)
{
    public async Task ExecuteAsync(EventConfigDto dto, CancellationToken ct = default)
    {
        var config = new EventConfig
        {
            RaceId = dto.RaceId,
            EventName = dto.EventName,
            RaceDurationMinutes = dto.RaceDurationMinutes,
            PitRules = dto.PitRules
                .Select(p => new PitRule { Name = p.Name, MinStopSeconds = p.MinStopSeconds, RequiredCount = p.RequiredCount })
                .ToList(),
            MonitoredKarts = dto.MonitoredKarts.ToList(),
            HasHeavyStintRule = dto.HasHeavyStintRule,
            HeavyStintRequiredCount = dto.HeavyStintRequiredCount,
            TrackTimeAlertMinutes = dto.TrackTimeAlertMinutes,
            AttentionLapSeconds = dto.AttentionLapSeconds,
            BoxLapSeconds = dto.BoxLapSeconds
        };
        await repository.SaveAsync(config, ct);
    }
}

public class SaveStintUseCase(IStintRepository repository)
{
    public async Task ExecuteAsync(int raceId, string kartNumber, StintDto dto, CancellationToken ct = default)
    {
        var stint = new Stint
        {
            Id = dto.Id,
            RaceId = raceId,
            KartNumber = kartNumber,
            Number = dto.Number,
            Driver = dto.Driver,
            EntryTime = dto.EntryTime,
            EntryLap = dto.EntryLap,
            ExitTime = dto.ExitTime,
            ExitLap = dto.ExitLap,
            StopTimeSeconds = dto.StopTimeSeconds,
            Heavy = dto.Heavy,
            IsManual = true
        };
        await repository.SaveAsync(stint, ct);
    }
}
