using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RaceOps.Domain.Entities;
using RaceOps.Domain.Interfaces;
using RaceOps.Infrastructure.RaceMonitor;

namespace RaceOps.Infrastructure.Live;

/// <summary>
/// Worker de uma corrida monitorada: faz o seed do estado via API REST
/// (sessão + histórico de voltas de cada kart), conecta ao WebSocket
/// RMonitor e mantém o estado atualizado. Se o stream cair, faz polling
/// da sessão até conseguir reconectar.
/// </summary>
internal sealed class RaceWorker : IDisposable
{
    private readonly int _raceId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly Action _notify;
    private readonly string? _replayFile;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _lock = new();

    private readonly Dictionary<string, KartLiveState> _karts = [];
    private string _sessionName = string.Empty;
    private string _trackName = string.Empty;
    private FlagStatus _flag = FlagStatus.Unknown;
    private string _currentTime = string.Empty;
    private string _sessionTime = string.Empty;
    private string _timeToGo = string.Empty;
    private string _lapsToGo = string.Empty;
    private bool _isStreaming;
    private bool _seeded;

    private DateTimeOffset _lastNotify = DateTimeOffset.MinValue;

    public RaceWorker(int raceId, IServiceScopeFactory scopeFactory, ILogger logger, Action notify, string? replayFile = null)
    {
        _raceId = raceId;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _notify = notify;
        _replayFile = replayFile;
    }

    public void Start() => _ = Task.Run(RunAsync);

    public LiveRaceSnapshot Snapshot()
    {
        lock (_lock)
        {
            return new LiveRaceSnapshot
            {
                RaceId = _raceId,
                SessionName = _sessionName,
                TrackName = _trackName,
                FlagStatus = _flag,
                CurrentTime = _currentTime,
                SessionTime = _sessionTime,
                TimeToGo = _timeToGo,
                LapsToGo = _lapsToGo,
                CapturedAt = DateTimeOffset.UtcNow,
                IsStreaming = _isStreaming,
                Karts = _karts.Values.Select(CloneKart).ToList()
            };
        }
    }

    private static KartLiveState CloneKart(KartLiveState k)
    {
        var clone = new KartLiveState
        {
            RacerId = k.RacerId,
            Number = k.Number,
            TeamName = k.TeamName,
            DriverName = k.DriverName,
            ClassId = k.ClassId,
            TimingPosition = k.TimingPosition,
            CurrentLap = k.CurrentLap,
            TotalTimeSeconds = k.TotalTimeSeconds,
            BestLapSeconds = k.BestLapSeconds,
            LastLapSeconds = k.LastLapSeconds,
            LastPassAt = k.LastPassAt
        };
        clone.Laps.AddRange(k.Laps);
        return clone;
    }

    private async Task RunAsync()
    {
        var ct = _cts.Token;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!_seeded)
                {
                    await SeedAsync(ct);
                    _seeded = true;
                    Notify(force: true);
                }

                var streamed = await TryStreamAsync(ct);
                if (!streamed)
                    await PollAsync(TimeSpan.FromSeconds(30), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro no monitoramento da corrida {RaceId}; tentando novamente em 5s", _raceId);
                try { await Task.Delay(TimeSpan.FromSeconds(5), ct); }
                catch (OperationCanceledException) { return; }
            }
        }
    }

    // --- Seed: sessão atual + histórico de voltas de cada kart ---

    private async Task SeedAsync(CancellationToken ct)
    {
        // Replay: o arquivo já contém o estado inicial; não precisamos da API REST.
        if (_replayFile != null) return;

        using var scope = _scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IRaceMonitorClient>();

        var session = await client.GetSessionAsync(_raceId, ct);
        ApplySession(session);

        // Histórico de voltas, kart a kart (uma vez no seed)
        foreach (var competitor in session.Competitors.Values)
        {
            if (string.IsNullOrEmpty(competitor.RacerId) || !int.TryParse(competitor.Laps, out var laps) || laps == 0)
                continue;

            try
            {
                var detail = await client.GetRacerAsync(_raceId, competitor.RacerId, ct);
                lock (_lock)
                {
                    if (_karts.TryGetValue(competitor.RacerId, out var kart))
                    {
                        // Mescla o histórico sem duplicar voltas já registradas
                        var known = kart.Laps.Select(l => l.Lap).ToHashSet();
                        foreach (var lap in detail.Laps)
                        {
                            if (int.TryParse(lap.Lap, out var lapNumber) && !known.Contains(lapNumber))
                            {
                                var seconds = TimeParsing.ToSeconds(lap.Time);
                                if (seconds > 0)
                                    kart.Laps.Add(new LapRecord(lapNumber, seconds));
                            }
                        }
                        kart.Laps.Sort((a, b) => a.Lap.CompareTo(b.Lap));
                    }
                }
                await Task.Delay(150, ct); // espaçamento para respeitar rate limit
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Falha ao carregar voltas do kart {RacerId}", competitor.RacerId);
            }
        }

        _logger.LogInformation("Seed da corrida {RaceId} concluído: {Karts} karts", _raceId, _karts.Count);
    }

    private void ApplySession(LiveSession session)
    {
        lock (_lock)
        {
            _sessionName = session.SessionName;
            _trackName = session.TrackName;
            _flag = session.FlagStatus;
            _currentTime = session.CurrentTime;
            _sessionTime = session.SessionTime;
            _timeToGo = session.TimeToGo;
            _lapsToGo = session.LapsToGo;

            foreach (var competitor in session.Competitors.Values)
            {
                if (string.IsNullOrEmpty(competitor.RacerId))
                    continue;

                if (!_karts.TryGetValue(competitor.RacerId, out var kart))
                {
                    kart = new KartLiveState { RacerId = competitor.RacerId };
                    _karts[competitor.RacerId] = kart;
                }

                kart.Number = competitor.Number;
                kart.DriverName = competitor.FullName;
                kart.TeamName = competitor.Nationality;
                kart.ClassId = competitor.ClassId;
                kart.TimingPosition = int.TryParse(competitor.Position, out var pos) ? pos : 0;
                kart.BestLapSeconds = TimeParsing.ToSeconds(competitor.BestLapTime);
                kart.TotalTimeSeconds = TimeParsing.ToSeconds(competitor.TotalTime);

                var laps = int.TryParse(competitor.Laps, out var lapCount) ? lapCount : 0;
                var lastLap = TimeParsing.ToSeconds(competitor.LastLapTime);

                // Nova volta detectada via polling → registra no histórico
                if (laps > kart.CurrentLap && lastLap > 0)
                {
                    if (kart.Laps.Count == 0 || kart.Laps[^1].Lap < laps)
                        kart.Laps.Add(new LapRecord(laps, lastLap, DateTimeOffset.UtcNow));
                    kart.LastPassAt = DateTimeOffset.UtcNow;
                }

                kart.CurrentLap = Math.Max(kart.CurrentLap, laps);
                if (lastLap > 0)
                    kart.LastLapSeconds = lastLap;
                if (kart.LastPassAt == default && laps > 0)
                    kart.LastPassAt = DateTimeOffset.UtcNow;
            }
        }
    }

    // --- Polling (fallback quando o stream não está disponível) ---

    private async Task PollAsync(TimeSpan duration, CancellationToken ct)
    {
        var until = DateTimeOffset.UtcNow + duration;
        while (DateTimeOffset.UtcNow < until && !ct.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IRaceMonitorClient>();
            try
            {
                var session = await client.GetSessionAsync(_raceId, ct);
                ApplySession(session);
                Notify();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Polling falhou para a corrida {RaceId}", _raceId);
            }
            await Task.Delay(TimeSpan.FromSeconds(4), ct);
        }
    }

    // --- Stream RMonitor via WebSocket ---

    private async Task<bool> TryStreamAsync(CancellationToken ct)
    {
        if (_replayFile != null)
            return await ReplayFromFileAsync(ct);

        string websocketUrl;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IRaceMonitorClient>();
            var info = await client.GetStreamingConnectionAsync(_raceId, ct);
            if (!info.IsLive || string.IsNullOrEmpty(info.WebsocketUrl))
                return false;
            websocketUrl = info.WebsocketUrl;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "GetStreamingConnection falhou para a corrida {RaceId}", _raceId);
            return false;
        }

        using var ws = new ClientWebSocket();
        try
        {
            await ws.ConnectAsync(new Uri(websocketUrl), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Conexão WebSocket falhou: {Url}", websocketUrl);
            return false;
        }

        _logger.LogInformation("Stream conectado para a corrida {RaceId}", _raceId);
        lock (_lock) _isStreaming = true;

        using var recorder = StreamRecorder.Create(_raceId);
        if (recorder != null)
            _logger.LogInformation("Gravando stream em {File}", recorder.FilePath);

        try
        {
            var buffer = new byte[8192];
            var messageBuffer = new StringBuilder();

            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (!result.EndOfMessage)
                    continue;

                var lines = messageBuffer.ToString();
                messageBuffer.Clear();

                foreach (var line in lines.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                {
                    recorder?.Record(line);
                    HandleMessage(RMonitorParser.Parse(line));
                }

                recorder?.Flush();
                Notify();
            }
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Stream interrompido para a corrida {RaceId}", _raceId);
            return false;
        }
        finally
        {
            lock (_lock) _isStreaming = false;
        }
    }

    // --- Replay a partir de arquivo gravado ---

    private async Task<bool> ReplayFromFileAsync(CancellationToken ct)
    {
        if (!File.Exists(_replayFile))
        {
            _logger.LogWarning("Arquivo de replay não encontrado: {File}", _replayFile);
            return false;
        }

        _logger.LogInformation("Iniciando replay da corrida {RaceId} a partir de {File}", _raceId, _replayFile);
        lock (_lock) _isStreaming = true;

        try
        {
            var rawLines = await File.ReadAllLinesAsync(_replayFile!, ct);
            long? prevTs = null;

            foreach (var raw in rawLines)
            {
                if (ct.IsCancellationRequested) break;
                var sep = raw.IndexOf('|');
                if (sep < 0) continue;

                if (long.TryParse(raw.AsSpan(0, sep), out var ts))
                {
                    if (prevTs.HasValue)
                    {
                        var delay = (int)Math.Clamp(ts - prevTs.Value, 0, 2000);
                        if (delay > 10) await Task.Delay(delay, ct);
                    }
                    prevTs = ts;
                }

                HandleMessage(RMonitorParser.Parse(raw[(sep + 1)..].Trim()));
                Notify();
            }

            _logger.LogInformation("Replay concluído para a corrida {RaceId}; mantendo último estado", _raceId);
            // Aguarda indefinidamente mantendo o estado capturado, sem chamar API
            await Task.Delay(Timeout.Infinite, ct);
            return true;
        }
        catch (OperationCanceledException) { return false; }
        finally
        {
            lock (_lock) _isStreaming = false;
        }
    }

    private void HandleMessage(RMonitorMessage message)
    {
        lock (_lock)
        {
            switch (message.Command)
            {
                // $A,regNo,number,transponder,firstName,lastName,nationality,classId
                case "$A":
                {
                    var kart = GetOrAddKart(message.Get(0));
                    kart.Number = message.Get(1);
                    var name = $"{message.Get(3)} {message.Get(4)}".Trim();
                    if (name.Length > 0) kart.DriverName = name;
                    kart.TeamName = message.Get(5);
                    kart.ClassId = message.Get(6);
                    break;
                }
                // $COMP,regNo,number,classId,firstName,lastName,nationality,additional
                case "$COMP":
                {
                    var kart = GetOrAddKart(message.Get(0));
                    kart.Number = message.Get(1);
                    kart.ClassId = message.Get(2);
                    var name = $"{message.Get(3)} {message.Get(4)}".Trim();
                    if (name.Length > 0) kart.DriverName = name;
                    kart.TeamName = message.Get(5);
                    break;
                }
                // $B,runId,"sessionName"
                case "$B":
                    _sessionName = message.Get(1);
                    break;
                // $E,"TRACKNAME","name"
                case "$E" when message.Get(0) == "TRACKNAME":
                    _trackName = message.Get(1);
                    break;
                // $F,lapsToGo,"timeToGo","timeOfDay","raceTime","flagStatus"
                case "$F":
                    _lapsToGo = message.Get(0);
                    _timeToGo = message.Get(1);
                    _currentTime = message.Get(2);
                    _sessionTime = message.Get(3);
                    _flag = ParseFlag(message.Get(4));
                    break;
                // $G,position,regNo,laps,"totalTime"
                case "$G":
                {
                    var kart = GetOrAddKart(message.Get(1));
                    if (int.TryParse(message.Get(0), out var position))
                        kart.TimingPosition = position;
                    if (int.TryParse(message.Get(2), out var laps))
                        kart.CurrentLap = Math.Max(kart.CurrentLap, laps);
                    var total = TimeParsing.ToSeconds(message.Get(3));
                    if (total > 0) kart.TotalTimeSeconds = total;
                    break;
                }
                // $H,position,regNo,bestLap,"bestLapTime"
                case "$H":
                {
                    var kart = GetOrAddKart(message.Get(1));
                    var best = TimeParsing.ToSeconds(message.Get(3));
                    if (best > 0) kart.BestLapSeconds = best;
                    break;
                }
                // $J,regNo,"lapTime","totalTime"
                case "$J":
                {
                    var kart = GetOrAddKart(message.Get(0));
                    var lapTime = TimeParsing.ToSeconds(message.Get(1));
                    var total = TimeParsing.ToSeconds(message.Get(2));
                    if (lapTime > 0)
                    {
                        var lapNumber = kart.Laps.Count > 0 ? kart.Laps[^1].Lap + 1 : kart.CurrentLap + 1;
                        kart.Laps.Add(new LapRecord(lapNumber, lapTime, DateTimeOffset.UtcNow));
                        kart.LastLapSeconds = lapTime;
                        kart.LastPassAt = DateTimeOffset.UtcNow;
                        kart.CurrentLap = Math.Max(kart.CurrentLap, lapNumber);
                    }
                    if (total > 0) kart.TotalTimeSeconds = total;
                    break;
                }
                // $I — reset de sessão
                case "$I":
                    _karts.Clear();
                    break;
            }
        }
    }

    private KartLiveState GetOrAddKart(string racerId)
    {
        if (!_karts.TryGetValue(racerId, out var kart))
        {
            kart = new KartLiveState { RacerId = racerId };
            _karts[racerId] = kart;
        }
        return kart;
    }

    private static FlagStatus ParseFlag(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "green" => FlagStatus.Green,
        "yellow" => FlagStatus.Yellow,
        "red" => FlagStatus.Red,
        "finish" => FlagStatus.Finish,
        _ => FlagStatus.Unknown
    };

    /// <summary>Notifica assinantes no máximo 1x por segundo.</summary>
    private void Notify(bool force = false)
    {
        var now = DateTimeOffset.UtcNow;
        if (!force && (now - _lastNotify).TotalMilliseconds < 1000)
            return;
        _lastNotify = now;
        _notify();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
