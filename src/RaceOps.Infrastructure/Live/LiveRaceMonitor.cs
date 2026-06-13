using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RaceOps.Domain.Interfaces;

namespace RaceOps.Infrastructure.Live;

/// <summary>
/// Mantém um worker de monitoramento por corrida. Cada worker conecta ao
/// stream RMonitor (WebSocket) com fallback de polling e acumula o estado
/// live dos karts, incluindo histórico de voltas.
/// </summary>
public class LiveRaceMonitor(IServiceScopeFactory scopeFactory, ILoggerFactory loggerFactory)
    : ILiveRaceMonitor, IDisposable
{
    private readonly ConcurrentDictionary<int, RaceWorker> _workers = new();

    public event Action<int>? StateUpdated;

    public void EnsureMonitoring(int raceId)
    {
        _workers.GetOrAdd(raceId, id =>
        {
            var worker = new RaceWorker(
                id, scopeFactory,
                loggerFactory.CreateLogger($"RaceWorker[{id}]"),
                () => StateUpdated?.Invoke(id));
            worker.Start();
            return worker;
        });
    }

    public void StopMonitoring(int raceId)
    {
        if (_workers.TryRemove(raceId, out var worker))
            worker.Dispose();
    }

    public LiveRaceSnapshot? GetSnapshot(int raceId)
        => _workers.TryGetValue(raceId, out var worker) ? worker.Snapshot() : null;

    public void Dispose()
    {
        foreach (var worker in _workers.Values)
            worker.Dispose();
        _workers.Clear();
        GC.SuppressFinalize(this);
    }
}
