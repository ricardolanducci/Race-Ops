namespace RaceOps.Domain.Entities;

/// <summary>Status calculado de um kart, conforme regras da planilha de colunas.</summary>
public enum KartStatus
{
    /// <summary>Verde — em pista.</summary>
    OnTrack,
    /// <summary>Amarelo — próximo de parar (tempo de pista acima do alerta).</summary>
    NearStop,
    /// <summary>Laranja — última volta acima do threshold de atenção.</summary>
    Attention,
    /// <summary>Vermelho — última volta indica passagem pelo box.</summary>
    Box
}

/// <summary>
/// Linha calculada da Classificação, com todas as colunas derivadas
/// definidas na planilha RaceOps-Colunas (aba Cronometro).
/// </summary>
public class StandingsRow
{
    public string RacerId { get; set; } = string.Empty;
    public string Kart { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;

    public int RealPosition { get; set; }
    public int TrackPosition { get; set; }

    /// <summary>Diferença para o kart imediatamente à frente na posição real (segundos), ou voltas.</summary>
    public double? GapSeconds { get; set; }
    public int? GapLaps { get; set; }

    public double LastLapSeconds { get; set; }
    public int LastLapRank { get; set; }
    public int CurrentLap { get; set; }
    public double? Avg5Seconds { get; set; }
    public double TrackTimeSeconds { get; set; }
    public double? StintAvgSeconds { get; set; }
    public int StintAvgRank { get; set; }
    public double? BestStintLapSeconds { get; set; }
    public double BestLapSeconds { get; set; }
    public double TotalTimeSeconds { get; set; }

    /// <summary>Contagem de paradas válidas por regra, na ordem das regras configuradas.</summary>
    public List<int> StopsPerRule { get; set; } = [];

    public KartStatus Status { get; set; }

    /// <summary>Voltas da stint atual (após a última parada detectada).</summary>
    public int StintLapCount { get; set; }
}
