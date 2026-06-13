using RaceOps.Application.DTOs;
using RaceOps.Domain.Entities;

namespace RaceOps.Application.UseCases;

/// <summary>
/// Constrói as linhas do gerenciador de stints de um kart: stints detectadas
/// automaticamente pelo histórico de voltas (volta acima do threshold de
/// parada fecha a stint) mescladas com edições manuais persistidas.
/// </summary>
public static class StintBuilder
{
    public static List<StintDto> Build(
        KartLiveState kart,
        EventConfig config,
        IReadOnlyList<Stint> manualStints,
        DateTimeOffset now)
    {
        var threshold = config.PitDetectionThresholdSeconds;
        var auto = BuildAutoStints(kart, threshold, now);

        // Edições manuais sobrescrevem a stint de mesmo número
        var manualByNumber = manualStints.ToDictionary(s => s.Number);
        var result = new List<StintDto>();

        var estimated = EstimatedSeconds(config);

        foreach (var stint in auto)
        {
            if (manualByNumber.TryGetValue(stint.Number, out var manual))
            {
                result.Add(new StintDto(
                    manual.Id,
                    stint.Number,
                    manual.Driver.Length > 0 ? manual.Driver : stint.Driver,
                    manual.EntryTime ?? stint.EntryTime,
                    manual.EntryLap ?? stint.EntryLap,
                    manual.ExitTime ?? stint.ExitTime,
                    manual.ExitLap ?? stint.ExitLap,
                    manual.StopTimeSeconds ?? stint.StopTimeSeconds,
                    manual.Heavy,
                    true,
                    stint.TrackTimeSeconds,
                    estimated,
                    stint.IsCurrent));
                manualByNumber.Remove(stint.Number);
            }
            else
            {
                result.Add(stint with { EstimatedSeconds = estimated });
            }
        }

        // Stints adicionadas manualmente além das detectadas (planejamento futuro)
        foreach (var manual in manualByNumber.Values.OrderBy(s => s.Number))
        {
            result.Add(new StintDto(
                manual.Id, manual.Number, manual.Driver,
                manual.EntryTime, manual.EntryLap, manual.ExitTime, manual.ExitLap,
                manual.StopTimeSeconds, manual.Heavy, true,
                null, estimated, false));
        }

        return result.OrderBy(s => s.Number).ToList();
    }

    private static double? EstimatedSeconds(EventConfig config)
    {
        var totalStops = config.TotalRequiredStops;
        if (totalStops == 0 || config.RaceDurationMinutes == 0)
            return null;
        return config.RaceDurationMinutes * 60.0 / (totalStops + 1);
    }

    private static List<StintDto> BuildAutoStints(KartLiveState kart, double threshold, DateTimeOffset now)
    {
        var stints = new List<StintDto>();
        if (kart.Laps.Count == 0)
            return stints;

        var number = 1;
        int? entryLap = kart.Laps[0].Lap;
        DateTimeOffset? entryTime = null;
        double trackTime = 0;

        foreach (var lap in kart.Laps)
        {
            if (lap.Seconds >= threshold)
            {
                // Volta de parada: fecha a stint corrente.
                // Hora de saída = hora da passagem - tempo de parada (planilha)
                var exitTime = lap.At?.AddSeconds(-lap.Seconds);
                stints.Add(new StintDto(
                    0, number, string.Empty,
                    entryTime, entryLap, exitTime, lap.Lap - 1,
                    lap.Seconds, false, false,
                    trackTime, null, false));

                number++;
                entryLap = lap.Lap;
                entryTime = lap.At; // Hora de entrada = saída + tempo de parada
                trackTime = 0;
            }
            else
            {
                trackTime += lap.Seconds;
            }
        }

        // Stint corrente (aberta) — piloto atual da cronometragem
        var running = kart.LastPassAt == default ? 0 : Math.Max(0, (now - kart.LastPassAt).TotalSeconds);
        stints.Add(new StintDto(
            0, number, kart.DriverName,
            entryTime, entryLap, null, null,
            null, false, false,
            trackTime + running, null, true));

        return stints;
    }
}
