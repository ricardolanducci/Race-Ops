using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RaceOps.Domain.Entities;
using RaceOps.Domain.Interfaces;
using RaceOps.Infrastructure.Database.Models;

namespace RaceOps.Infrastructure.Database;

public class EventConfigRepository(AppDbContext db) : IEventConfigRepository
{
    public async Task<EventConfig?> GetAsync(int raceId, CancellationToken ct = default)
    {
        var model = await db.EventConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.RaceId == raceId, ct);
        return model is null ? null : ToEntity(model);
    }

    public async Task SaveAsync(EventConfig config, CancellationToken ct = default)
    {
        var model = await db.EventConfigs.FirstOrDefaultAsync(c => c.RaceId == config.RaceId, ct);
        if (model is null)
        {
            model = new EventConfigModel { RaceId = config.RaceId };
            db.EventConfigs.Add(model);
        }

        model.EventName = config.EventName;
        model.RaceDurationMinutes = config.RaceDurationMinutes;
        model.PitRulesJson = JsonSerializer.Serialize(config.PitRules);
        model.MonitoredKartsJson = JsonSerializer.Serialize(config.MonitoredKarts);
        model.HasHeavyStintRule = config.HasHeavyStintRule;
        model.HeavyStintRequiredCount = config.HeavyStintRequiredCount;
        model.TrackTimeAlertMinutes = config.TrackTimeAlertMinutes;
        model.AttentionLapSeconds = config.AttentionLapSeconds;
        model.BoxLapSeconds = config.BoxLapSeconds;

        await db.SaveChangesAsync(ct);
    }

    private static EventConfig ToEntity(EventConfigModel model) => new()
    {
        RaceId = model.RaceId,
        EventName = model.EventName,
        RaceDurationMinutes = model.RaceDurationMinutes,
        PitRules = JsonSerializer.Deserialize<List<PitRule>>(model.PitRulesJson) ?? [],
        MonitoredKarts = JsonSerializer.Deserialize<List<string>>(model.MonitoredKartsJson) ?? [],
        HasHeavyStintRule = model.HasHeavyStintRule,
        HeavyStintRequiredCount = model.HeavyStintRequiredCount,
        TrackTimeAlertMinutes = model.TrackTimeAlertMinutes,
        AttentionLapSeconds = model.AttentionLapSeconds,
        BoxLapSeconds = model.BoxLapSeconds
    };
}

public class StintRepository(AppDbContext db) : IStintRepository
{
    public async Task<IReadOnlyList<Stint>> GetByKartAsync(int raceId, string kartNumber, CancellationToken ct = default)
        => await Query().Where(s => s.RaceId == raceId && s.KartNumber == kartNumber)
            .OrderBy(s => s.Number).Select(s => ToEntity(s)).ToListAsync(ct);

    public async Task<IReadOnlyList<Stint>> GetByRaceAsync(int raceId, CancellationToken ct = default)
        => await Query().Where(s => s.RaceId == raceId)
            .OrderBy(s => s.KartNumber).ThenBy(s => s.Number)
            .Select(s => ToEntity(s)).ToListAsync(ct);

    public async Task SaveAsync(Stint stint, CancellationToken ct = default)
    {
        var model = stint.Id > 0
            ? await db.Stints.FirstOrDefaultAsync(s => s.Id == stint.Id, ct)
            : await db.Stints.FirstOrDefaultAsync(
                s => s.RaceId == stint.RaceId && s.KartNumber == stint.KartNumber && s.Number == stint.Number, ct);

        if (model is null)
        {
            model = new StintModel();
            db.Stints.Add(model);
        }

        model.RaceId = stint.RaceId;
        model.KartNumber = stint.KartNumber;
        model.Number = stint.Number;
        model.Driver = stint.Driver;
        model.EntryTime = stint.EntryTime;
        model.EntryLap = stint.EntryLap;
        model.ExitTime = stint.ExitTime;
        model.ExitLap = stint.ExitLap;
        model.StopTimeSeconds = stint.StopTimeSeconds;
        model.Heavy = stint.Heavy;
        model.IsManual = stint.IsManual;

        await db.SaveChangesAsync(ct);
        stint.Id = model.Id;
    }

    public async Task DeleteAsync(int stintId, CancellationToken ct = default)
    {
        var model = await db.Stints.FirstOrDefaultAsync(s => s.Id == stintId, ct);
        if (model is not null)
        {
            db.Stints.Remove(model);
            await db.SaveChangesAsync(ct);
        }
    }

    private IQueryable<StintModel> Query() => db.Stints.AsNoTracking();

    private static Stint ToEntity(StintModel model) => new()
    {
        Id = model.Id,
        RaceId = model.RaceId,
        KartNumber = model.KartNumber,
        Number = model.Number,
        Driver = model.Driver,
        EntryTime = model.EntryTime,
        EntryLap = model.EntryLap,
        ExitTime = model.ExitTime,
        ExitLap = model.ExitLap,
        StopTimeSeconds = model.StopTimeSeconds,
        Heavy = model.Heavy,
        IsManual = model.IsManual
    };
}
