using RaceOps.Application.DTOs;
using RaceOps.Domain.Entities;
using RaceOps.Domain.Interfaces;
using RaceOps.Domain.Services;

namespace RaceOps.Application.UseCases;

/// <summary>
/// Monta o LiveViewDto completo a partir do snapshot live, da configuração
/// do evento e das stints persistidas. Usado tanto pelo endpoint REST de
/// carga inicial quanto pelo broadcast SignalR.
/// </summary>
public class GetLiveViewUseCase(
    ILiveRaceMonitor monitor,
    IEventConfigRepository configRepository,
    IStintRepository stintRepository)
{
    public async Task<LiveViewDto?> ExecuteAsync(int raceId, CancellationToken ct = default)
    {
        monitor.EnsureMonitoring(raceId);

        var snapshot = monitor.GetSnapshot(raceId);
        if (snapshot is null)
            return null;

        var config = await configRepository.GetAsync(raceId, ct) ?? DefaultConfig(raceId);
        var manualStints = await stintRepository.GetByRaceAsync(raceId, ct);
        var now = DateTimeOffset.UtcNow;

        var rows = StandingsCalculator.Compute(snapshot.Karts, config, now);
        var monitoredSet = config.MonitoredKarts.Select(NormalizeKart).ToHashSet();

        var standings = rows.Select(r => ToRowDto(r, monitoredSet)).ToList();

        var avg5Ranked = rows.Where(r => r.Avg5Seconds.HasValue)
            .OrderBy(r => r.Avg5Seconds!.Value)
            .Select((r, i) => (r.RacerId, Rank: i + 1))
            .ToDictionary(x => x.RacerId, x => x.Rank);

        var monitored = rows
            .Where(r => monitoredSet.Contains(NormalizeKart(r.Kart)))
            .OrderBy(r => NormalizeKart(r.Kart))
            .Select(r => BuildKartModule(r, rows, snapshot, config, manualStints, avg5Ranked, now))
            .ToList();

        return new LiveViewDto(
            raceId,
            snapshot.SessionName,
            snapshot.TrackName,
            snapshot.FlagStatus.ToString(),
            snapshot.CurrentTime,
            snapshot.SessionTime,
            snapshot.TimeToGo,
            snapshot.LapsToGo,
            snapshot.IsStreaming,
            BuildRaceData(rows),
            config.PitRules.Select(p => p.Name).ToList(),
            standings,
            monitored);
    }

    public static EventConfig DefaultConfig(int raceId) => new() { RaceId = raceId };

    private static string NormalizeKart(string kart) => kart.TrimStart('0').Length == 0
        ? "0"
        : kart.TrimStart('0');

    private static StandingsRowDto ToRowDto(StandingsRow row, HashSet<string> monitoredSet) => new(
        row.RacerId,
        row.RealPosition,
        row.TrackPosition,
        row.Kart,
        row.Team,
        row.Driver,
        FormatGap(row),
        row.LastLapSeconds,
        row.LastLapRank,
        row.CurrentLap,
        row.Avg5Seconds,
        row.TrackTimeSeconds,
        row.StintAvgSeconds,
        row.StintAvgRank,
        row.StopsPerRule,
        row.Status.ToString(),
        monitoredSet.Contains(NormalizeKart(row.Kart)));

    private static string FormatGap(StandingsRow row)
    {
        if (row.RealPosition == 1) return "-";
        if (row.GapLaps is > 0) return $"+{row.GapLaps} V";
        if (row.GapSeconds.HasValue)
            return "+" + row.GapSeconds.Value.ToString("00.000", System.Globalization.CultureInfo.InvariantCulture);
        return "--";
    }

    private static RaceDataDto BuildRaceData(List<StandingsRow> rows)
    {
        var lapAverages = rows.Where(r => r.Avg5Seconds.HasValue).Select(r => r.Avg5Seconds!.Value).ToList();
        var top10 = rows.Where(r => r.Avg5Seconds.HasValue && r.RealPosition <= 10)
            .Select(r => r.Avg5Seconds!.Value).ToList();

        return new RaceDataDto(
            lapAverages.Count > 0 ? lapAverages.Average() : null,
            top10.Count > 0 ? top10.Average() : null);
    }

    private KartModuleDto BuildKartModule(
        StandingsRow row,
        List<StandingsRow> allRows,
        LiveRaceSnapshot snapshot,
        EventConfig config,
        IReadOnlyList<Stint> manualStints,
        Dictionary<string, int> avg5Ranked,
        DateTimeOffset now)
    {
        var kart = snapshot.Karts.First(k => k.RacerId == row.RacerId);
        var threshold = config.PitDetectionThresholdSeconds;

        var recentLaps = BuildRecentLaps(kart, snapshot, threshold);
        var stints = StintBuilder.Build(kart, config,
            manualStints.Where(s => s.KartNumber == row.Kart).ToList(), now);

        return new KartModuleDto(
            row.Kart,
            row.RacerId,
            row.Driver,
            row.Team,
            row.RealPosition,
            row.TrackPosition,
            row.CurrentLap,
            row.Status.ToString(),
            kart.LastPassAt == default ? 0 : Math.Max(0, (now - kart.LastPassAt).TotalSeconds),
            row.TrackTimeSeconds,
            EstimatedStintSeconds(config),
            row.BestStintLapSeconds,
            row.Avg5Seconds,
            avg5Ranked.GetValueOrDefault(row.RacerId),
            row.StintAvgSeconds,
            recentLaps,
            BuildRelative(row, allRows),
            stints,
            row.StopsPerRule);
    }

    private static double? EstimatedStintSeconds(EventConfig config)
    {
        var totalStops = config.TotalRequiredStops;
        if (totalStops == 0 || config.RaceDurationMinutes == 0)
            return null;
        return config.RaceDurationMinutes * 60.0 / (totalStops + 1);
    }

    private static List<RecentLapDto> BuildRecentLaps(KartLiveState kart, LiveRaceSnapshot snapshot, double threshold)
    {
        // Ranking de cada volta entre os tempos do grid naquela mesma volta
        var gridLaps = snapshot.Karts
            .SelectMany(k => k.Laps)
            .Where(l => l.Seconds > 0 && l.Seconds < threshold)
            .GroupBy(l => l.Lap)
            .ToDictionary(g => g.Key, g => g.Select(l => l.Seconds).OrderBy(s => s).ToList());

        var bestLap = kart.Laps.Where(l => l.Seconds > 0 && l.Seconds < threshold)
            .Select(l => l.Seconds).DefaultIfEmpty(0).Min();

        var result = new List<RecentLapDto>();
        var laps = kart.Laps;
        for (var i = Math.Max(0, laps.Count - 8); i < laps.Count; i++)
        {
            var lap = laps[i];
            double? delta = i > 0 ? lap.Seconds - laps[i - 1].Seconds : null;
            int? rank = null;
            if (lap.Seconds < threshold && gridLaps.TryGetValue(lap.Lap, out var times))
                rank = times.BinarySearch(lap.Seconds) is var idx && idx >= 0 ? idx + 1 : ~idx + 1;

            result.Add(new RecentLapDto(lap.Lap, lap.Seconds, delta, rank,
                bestLap > 0 && Math.Abs(lap.Seconds - bestLap) < 0.0005));
        }

        result.Reverse(); // mais recente primeiro, como no Figma
        return result;
    }

    private static RelativeDto BuildRelative(StandingsRow row, List<StandingsRow> allRows)
    {
        var byReal = allRows.OrderBy(r => r.RealPosition).ToList();
        var byTrack = allRows.Where(r => r.TrackPosition > 0).OrderBy(r => r.TrackPosition).ToList();

        return new RelativeDto(
            Neighbor(byReal, byReal.FindIndex(r => r.RacerId == row.RacerId) - 1, row),
            Neighbor(byReal, byReal.FindIndex(r => r.RacerId == row.RacerId) + 1, row),
            Neighbor(byTrack, byTrack.FindIndex(r => r.RacerId == row.RacerId) - 1, row),
            Neighbor(byTrack, byTrack.FindIndex(r => r.RacerId == row.RacerId) + 1, row));
    }

    private static RelativeEntryDto? Neighbor(List<StandingsRow> ordered, int index, StandingsRow reference)
    {
        if (index < 0 || index >= ordered.Count)
            return null;
        var other = ordered[index];
        double? gap = other.TotalTimeSeconds > 0 && reference.TotalTimeSeconds > 0
            ? Math.Abs(other.TotalTimeSeconds - reference.TotalTimeSeconds)
            : null;
        return new RelativeEntryDto(other.Kart, other.Team, other.LastLapSeconds, gap);
    }
}
