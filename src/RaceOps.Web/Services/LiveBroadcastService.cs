using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using RaceOps.Application.UseCases;
using RaceOps.Domain.Interfaces;
using RaceOps.Web.Hubs;

namespace RaceOps.Web.Services;

/// <summary>
/// Assina as atualizações do monitor live e re-transmite o LiveViewDto
/// completo para o grupo SignalR de cada corrida (no máximo ~1x/s,
/// já limitado pelo monitor).
/// </summary>
public class LiveBroadcastService(
    ILiveRaceMonitor monitor,
    IServiceScopeFactory scopeFactory,
    IHubContext<RaceHub> hub,
    ILogger<LiveBroadcastService> logger) : IHostedService
{
    private readonly ConcurrentDictionary<int, bool> _building = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        monitor.StateUpdated += OnStateUpdated;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        monitor.StateUpdated -= OnStateUpdated;
        return Task.CompletedTask;
    }

    private void OnStateUpdated(int raceId)
    {
        // Evita builds concorrentes para a mesma corrida
        if (!_building.TryAdd(raceId, true))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var useCase = scope.ServiceProvider.GetRequiredService<GetLiveViewUseCase>();
                var view = await useCase.ExecuteAsync(raceId);
                if (view is not null)
                    await hub.Clients.Group(RaceHub.RaceGroup(raceId)).SendAsync("LiveViewUpdated", view);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao transmitir live view da corrida {RaceId}", raceId);
            }
            finally
            {
                _building.TryRemove(raceId, out _);
            }
        });
    }
}
