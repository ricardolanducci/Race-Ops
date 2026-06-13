// hub.js — conexão SignalR com o servidor para dados em tempo real

const Hub = (() => {
  let connection = null;
  let currentRaceId = null;

  const handlers = {
    onLiveViewUpdated: null,
  };

  async function connect(raceId) {
    if (connection && currentRaceId !== null && currentRaceId !== raceId) {
      try { await connection.invoke('LeaveRace', currentRaceId); } catch { /* ignore */ }
    }

    if (!connection) {
      connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/race')
        .withAutomaticReconnect()
        .build();

      connection.on('LiveViewUpdated', (view) => handlers.onLiveViewUpdated?.(view));
      connection.onreconnected(() => {
        if (currentRaceId !== null) connection.invoke('JoinRace', currentRaceId);
      });

      await connection.start();
    }

    currentRaceId = raceId;
    await connection.invoke('JoinRace', raceId);
  }

  return {
    connect,
    on: (event, handler) => { handlers[event] = handler; },
  };
})();
