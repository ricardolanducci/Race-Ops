namespace RaceOps.Domain.Entities;

/// <summary>
/// Estado acumulado de um kart durante a sessão live: identificação,
/// posição de cronometragem e histórico de voltas. Alimentado pelo
/// stream RMonitor e/ou polling da API.
/// </summary>
public class KartLiveState
{
    public string RacerId { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;

    /// <summary>Posição registrada na cronometragem (Posição Pista).</summary>
    public int TimingPosition { get; set; }

    public int CurrentLap { get; set; }
    public double TotalTimeSeconds { get; set; }
    public double BestLapSeconds { get; set; }
    public double LastLapSeconds { get; set; }

    /// <summary>Hora (UTC local do servidor) em que a última volta foi registrada.</summary>
    public DateTimeOffset LastPassAt { get; set; }

    /// <summary>Histórico de voltas registradas nesta sessão (pode começar no meio).</summary>
    public List<LapRecord> Laps { get; } = [];
}

/// <param name="At">Hora em que a volta foi registrada pelo monitor (null para histórico pré-carregado).</param>
public record LapRecord(int Lap, double Seconds, DateTimeOffset? At = null);
