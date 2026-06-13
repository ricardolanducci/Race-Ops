using RaceOps.Domain.Entities;

namespace RaceOps.Domain.Interfaces;

/// <summary>
/// Snapshot imutável do estado live de uma corrida monitorada.
/// </summary>
public class LiveRaceSnapshot
{
    public int RaceId { get; init; }
    public string SessionName { get; init; } = string.Empty;
    public string TrackName { get; init; } = string.Empty;
    public FlagStatus FlagStatus { get; init; }
    public string CurrentTime { get; init; } = string.Empty;
    public string SessionTime { get; init; } = string.Empty;
    public string TimeToGo { get; init; } = string.Empty;
    public string LapsToGo { get; init; } = string.Empty;
    public DateTimeOffset CapturedAt { get; init; }
    public bool IsStreaming { get; init; }
    public IReadOnlyList<KartLiveState> Karts { get; init; } = [];
}

/// <summary>
/// Serviço que mantém o estado live das corridas monitoradas
/// (stream RMonitor + fallback de polling) e expõe snapshots.
/// </summary>
public interface ILiveRaceMonitor
{
    /// <summary>Inicia (ou mantém) o monitoramento de uma corrida.</summary>
    void EnsureMonitoring(int raceId);

    /// <summary>Para o monitoramento de uma corrida.</summary>
    void StopMonitoring(int raceId);

    /// <summary>Snapshot atual, ou null se a corrida não está sendo monitorada.</summary>
    LiveRaceSnapshot? GetSnapshot(int raceId);

    /// <summary>Disparado (no máximo ~1x/s por corrida) quando o estado muda.</summary>
    event Action<int>? StateUpdated;
}
