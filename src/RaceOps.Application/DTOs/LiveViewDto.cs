namespace RaceOps.Application.DTOs;

/// <summary>
/// Payload único enviado ao frontend (REST inicial + SignalR) com tudo
/// que as telas Visão Geral, Classificação e Estratégia precisam.
/// Tempos são enviados em segundos; o frontend formata.
/// </summary>
public record LiveViewDto(
    int RaceId,
    string SessionName,
    string TrackName,
    string FlagStatus,
    string CurrentTime,
    string SessionTime,
    string TimeToGo,
    string LapsToGo,
    bool IsStreaming,
    RaceDataDto RaceData,
    IReadOnlyList<string> StopRuleLabels,
    IReadOnlyList<StandingsRowDto> Standings,
    IReadOnlyList<KartModuleDto> Monitored
);

/// <summary>Barra "Dados de corrida".</summary>
public record RaceDataDto(
    double? AvgTrackLapSeconds,
    double? AvgTop10LapSeconds
);

/// <summary>Linha da Classificação com todas as colunas calculadas.</summary>
public record StandingsRowDto(
    string RacerId,
    int Real,
    int Pista,
    string Kart,
    string Team,
    string Driver,
    string Gap,
    double LastLapSeconds,
    int LastLapRank,
    int Lap,
    double? Avg5Seconds,
    double TrackTimeSeconds,
    double? StintAvgSeconds,
    int StintAvgRank,
    IReadOnlyList<int> Stops,
    string Status,
    bool IsMonitored
);

/// <summary>Dados do módulo Cronometro de um kart monitorado.</summary>
public record KartModuleDto(
    string Kart,
    string RacerId,
    string Driver,
    string Team,
    int Real,
    int Pista,
    int CurrentLap,
    string Status,
    double SecondsSinceLastPass,
    double TrackTimeSeconds,
    double? EstStintSeconds,
    double? BestStintLapSeconds,
    double? Avg5Seconds,
    int Avg5Rank,
    double? StintAvgSeconds,
    IReadOnlyList<RecentLapDto> RecentLaps,
    RelativeDto Relative,
    IReadOnlyList<StintDto> Stints,
    IReadOnlyList<int> Stops
);

public record RecentLapDto(int Lap, double Seconds, double? DeltaSeconds, int? Rank, bool IsBest);

public record RelativeDto(
    RelativeEntryDto? AheadReal,
    RelativeEntryDto? BehindReal,
    RelativeEntryDto? AheadTrack,
    RelativeEntryDto? BehindTrack
);

public record RelativeEntryDto(string Kart, string Team, double LastLapSeconds, double? GapSeconds);

/// <summary>Linha do gerenciador de stints.</summary>
public record StintDto(
    int Id,
    int Number,
    string Driver,
    DateTimeOffset? EntryTime,
    int? EntryLap,
    DateTimeOffset? ExitTime,
    int? ExitLap,
    double? StopTimeSeconds,
    bool Heavy,
    bool IsManual,
    double? TrackTimeSeconds,
    double? EstimatedSeconds,
    bool IsCurrent
);

/// <summary>Configuração do evento (GET/PUT).</summary>
public record EventConfigDto(
    int RaceId,
    string EventName,
    int RaceDurationMinutes,
    IReadOnlyList<PitRuleDto> PitRules,
    IReadOnlyList<string> MonitoredKarts,
    bool HasHeavyStintRule,
    int HeavyStintRequiredCount,
    int TrackTimeAlertMinutes,
    double AttentionLapSeconds,
    double BoxLapSeconds
);

public record PitRuleDto(string Name, double MinStopSeconds, int RequiredCount);
