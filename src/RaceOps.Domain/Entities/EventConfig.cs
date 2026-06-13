namespace RaceOps.Domain.Entities;

/// <summary>
/// Configuração do evento monitorado: regras de parada, karts da equipe
/// e thresholds usados nas colunas calculadas (Status, alertas).
/// </summary>
public class EventConfig
{
    public int RaceId { get; set; }
    public string EventName { get; set; } = string.Empty;

    /// <summary>Duração total da corrida em minutos (usada no Tempo Estimado de stint).</summary>
    public int RaceDurationMinutes { get; set; } = 720;

    /// <summary>Regras de parada cadastradas (ex: 9 paradas de 6min, 1 de 10min).</summary>
    public List<PitRule> PitRules { get; set; } = [];

    /// <summary>Números dos karts monitorados pela equipe (ex: 014, 015, 016).</summary>
    public List<string> MonitoredKarts { get; set; } = [];

    /// <summary>O evento possui regra de stint pesada (lastro)?</summary>
    public bool HasHeavyStintRule { get; set; }
    public int HeavyStintRequiredCount { get; set; }

    /// <summary>Status Amarelo: kart há mais que isso em pista está próximo de parar.</summary>
    public int TrackTimeAlertMinutes { get; set; } = 50;

    /// <summary>Status Laranja: última volta acima disso (segundos) é atenção.</summary>
    public double AttentionLapSeconds { get; set; } = 120;

    /// <summary>Status Vermelho: última volta acima disso (segundos) indica box.</summary>
    public double BoxLapSeconds { get; set; } = 180;

    /// <summary>Menor threshold de parada — voltas acima disso fecham a stint atual.</summary>
    public double PitDetectionThresholdSeconds =>
        PitRules.Count > 0 ? PitRules.Min(r => r.MinStopSeconds) : AttentionLapSeconds;

    public int TotalRequiredStops => PitRules.Sum(r => r.RequiredCount);
}

/// <summary>Regra de parada: tempo mínimo de volta para contar e quantidade obrigatória.</summary>
public class PitRule
{
    /// <summary>Rótulo curto exibido na coluna, ex: "06m", "10m".</summary>
    public string Name { get; set; } = string.Empty;
    public double MinStopSeconds { get; set; }
    public int RequiredCount { get; set; }
}
