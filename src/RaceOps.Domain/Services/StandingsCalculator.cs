using RaceOps.Domain.Entities;

namespace RaceOps.Domain.Services;

/// <summary>
/// Calcula as colunas derivadas da Classificação a partir do estado live
/// dos karts e da configuração do evento (planilha RaceOps-Colunas, aba Cronometro).
/// Lógica pura, sem dependências — totalmente testável.
/// </summary>
public static class StandingsCalculator
{
    public static List<StandingsRow> Compute(
        IReadOnlyCollection<KartLiveState> karts,
        EventConfig config,
        DateTimeOffset now)
    {
        var rows = karts.Select(k => BuildRow(k, config, now)).ToList();

        RankLastLaps(rows);
        RankStintAverages(rows);
        AssignRealPositions(rows, config);
        ComputeGaps(rows);

        return rows.OrderBy(r => r.RealPosition).ToList();
    }

    private static StandingsRow BuildRow(KartLiveState kart, EventConfig config, DateTimeOffset now)
    {
        var threshold = config.PitDetectionThresholdSeconds;

        // Voltas da stint atual: tudo após a última volta de parada detectada
        var lastPitIndex = kart.Laps.FindLastIndex(l => l.Seconds >= threshold);
        var stintLaps = kart.Laps.Skip(lastPitIndex + 1).Select(l => l.Seconds).ToList();

        // Contador de paradas por regra: cada volta de parada conta para a
        // regra de maior tempo mínimo que ela satisfaz
        var stops = config.PitRules.Select(_ => 0).ToList();
        if (config.PitRules.Count > 0)
        {
            var rulesByThreshold = config.PitRules
                .Select((rule, index) => (rule, index))
                .OrderByDescending(x => x.rule.MinStopSeconds)
                .ToList();

            foreach (var lap in kart.Laps.Where(l => l.Seconds >= threshold))
            {
                var match = rulesByThreshold.FirstOrDefault(x => lap.Seconds >= x.rule.MinStopSeconds);
                if (match.rule is not null)
                    stops[match.index]++;
            }
        }

        // Média das últimas 5 voltas válidas (desconsidera voltas de parada)
        var cleanLaps = kart.Laps.Where(l => l.Seconds > 0 && l.Seconds < threshold)
            .Select(l => l.Seconds).ToList();
        var last5 = cleanLaps.TakeLast(5).ToList();

        var runningLapSeconds = kart.LastPassAt == default
            ? 0
            : Math.Max(0, (now - kart.LastPassAt).TotalSeconds);

        var trackTime = stintLaps.Sum() + runningLapSeconds;

        return new StandingsRow
        {
            RacerId = kart.RacerId,
            Kart = kart.Number,
            Team = kart.TeamName,
            Driver = kart.DriverName,
            TrackPosition = kart.TimingPosition,
            CurrentLap = kart.CurrentLap,
            LastLapSeconds = kart.LastLapSeconds,
            BestLapSeconds = kart.BestLapSeconds,
            TotalTimeSeconds = kart.TotalTimeSeconds,
            Avg5Seconds = last5.Count > 0 ? last5.Average() : null,
            TrackTimeSeconds = trackTime,
            StintAvgSeconds = stintLaps.Count > 0 ? stintLaps.Average() : null,
            BestStintLapSeconds = stintLaps.Count > 0 ? stintLaps.Min() : null,
            StintLapCount = stintLaps.Count,
            StopsPerRule = stops,
            Status = ComputeStatus(runningLapSeconds, trackTime, config)
        };
    }

    /// <summary>
    /// Status conforme a planilha: Verde em pista, Amarelo próximo de parar
    /// (tempo de pista acima do alerta), Laranja volta acima da atenção,
    /// Vermelho volta indicando box. Usa a volta corrente em andamento para
    /// refletir o estado atual (um kart parado agora fica vermelho agora).
    /// </summary>
    private static KartStatus ComputeStatus(double runningLapSeconds, double trackTimeSeconds, EventConfig config)
    {
        if (runningLapSeconds >= config.BoxLapSeconds)
            return KartStatus.Box;
        if (runningLapSeconds >= config.AttentionLapSeconds)
            return KartStatus.Attention;
        if (trackTimeSeconds >= config.TrackTimeAlertMinutes * 60)
            return KartStatus.NearStop;
        return KartStatus.OnTrack;
    }

    private static void RankLastLaps(List<StandingsRow> rows)
    {
        var ranked = rows.Where(r => r.LastLapSeconds > 0)
            .OrderBy(r => r.LastLapSeconds).ToList();
        for (var i = 0; i < ranked.Count; i++)
            ranked[i].LastLapRank = i + 1;
    }

    private static void RankStintAverages(List<StandingsRow> rows)
    {
        var ranked = rows.Where(r => r.StintAvgSeconds.HasValue)
            .OrderBy(r => r.StintAvgSeconds!.Value).ToList();
        for (var i = 0; i < ranked.Count; i++)
            ranked[i].StintAvgRank = i + 1;
    }

    /// <summary>
    /// Posição Real: quem está mais próximo de cumprir todas as regras de
    /// parada vem antes; empate decidido por mais voltas e menor tempo total.
    /// </summary>
    private static void AssignRealPositions(List<StandingsRow> rows, EventConfig config)
    {
        var ordered = rows
            .OrderBy(r => RemainingStops(r, config))
            .ThenByDescending(r => r.CurrentLap)
            .ThenBy(r => r.TotalTimeSeconds > 0 ? r.TotalTimeSeconds : double.MaxValue)
            .ThenBy(r => r.TrackPosition)
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
            ordered[i].RealPosition = i + 1;
    }

    private static int RemainingStops(StandingsRow row, EventConfig config)
    {
        var remaining = 0;
        for (var i = 0; i < config.PitRules.Count; i++)
        {
            var done = i < row.StopsPerRule.Count ? row.StopsPerRule[i] : 0;
            remaining += Math.Max(0, config.PitRules[i].RequiredCount - done);
        }
        return remaining;
    }

    /// <summary>Diferença para o kart imediatamente à frente na posição real.</summary>
    private static void ComputeGaps(List<StandingsRow> rows)
    {
        var ordered = rows.OrderBy(r => r.RealPosition).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            var ahead = ordered[i - 1];
            var current = ordered[i];

            if (current.CurrentLap == ahead.CurrentLap && current.TotalTimeSeconds > 0 && ahead.TotalTimeSeconds > 0)
                current.GapSeconds = current.TotalTimeSeconds - ahead.TotalTimeSeconds;
            else if (current.CurrentLap < ahead.CurrentLap)
                current.GapLaps = ahead.CurrentLap - current.CurrentLap;
        }
    }
}
