using Microsoft.AspNetCore.SignalR;
using RaceOps.Application.DTOs;

namespace RaceOps.Web.Hubs;

/// <summary>
/// Hub SignalR para transmitir dados de corrida em tempo real para o browser.
/// Clientes se conectam e entram no grupo da corrida que querem acompanhar.
/// </summary>
public class RaceHub(RaceOps.Domain.Interfaces.ILiveRaceMonitor monitor) : Hub
{
    /// <summary>
    /// Chamado pelo cliente para entrar no grupo de uma corrida.
    /// Após entrar, o cliente recebe todos os eventos daquela corrida.
    /// Entrar no grupo também garante que o backend está monitorando a corrida.
    /// </summary>
    public async Task JoinRace(int raceId)
    {
        monitor.EnsureMonitoring(raceId);
        await Groups.AddToGroupAsync(Context.ConnectionId, RaceGroup(raceId));
    }

    /// <summary>
    /// Chamado pelo cliente para sair do grupo de uma corrida.
    /// </summary>
    public async Task LeaveRace(int raceId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, RaceGroup(raceId));

    public static string RaceGroup(int raceId) => $"race-{raceId}";
}

/// <summary>
/// Extensões para enviar mensagens tipadas aos clientes do hub.
/// Usado pelo serviço de streaming para notificar o browser.
/// </summary>
public static class RaceHubExtensions
{
    public static Task SendSessionUpdated(this IHubContext<RaceHub> hub, int raceId, LiveSessionDto session)
        => hub.Clients.Group(RaceHub.RaceGroup(raceId)).SendAsync("SessionUpdated", session);

    public static Task SendFlagChanged(this IHubContext<RaceHub> hub, int raceId, string flagStatus, string timeToGo)
        => hub.Clients.Group(RaceHub.RaceGroup(raceId)).SendAsync("FlagChanged", flagStatus, timeToGo);

    public static Task SendCompetitorPassed(this IHubContext<RaceHub> hub, int raceId, string racerId, string lapTime, string totalTime)
        => hub.Clients.Group(RaceHub.RaceGroup(raceId)).SendAsync("CompetitorPassed", racerId, lapTime, totalTime);
}
