namespace RaceOps.Domain.Entities;

/// <summary>
/// Stint de um kart no gerenciador de estratégia (aba Gerenciador da planilha).
/// Linhas podem ser geradas automaticamente (detecção de parada) ou editadas manualmente.
/// </summary>
public class Stint
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public string KartNumber { get; set; } = string.Empty;

    /// <summary>Número sequencial da stint (1 = largada).</summary>
    public int Number { get; set; }

    public string Driver { get; set; } = string.Empty;

    public DateTimeOffset? EntryTime { get; set; }
    public int? EntryLap { get; set; }
    public DateTimeOffset? ExitTime { get; set; }
    public int? ExitLap { get; set; }

    /// <summary>Tempo da volta em que a parada foi contabilizada (segundos).</summary>
    public double? StopTimeSeconds { get; set; }

    /// <summary>Stint feita com lastro (regra de stint pesada).</summary>
    public bool Heavy { get; set; }

    /// <summary>Campos editados manualmente não são sobrescritos pela detecção automática.</summary>
    public bool IsManual { get; set; }
}
