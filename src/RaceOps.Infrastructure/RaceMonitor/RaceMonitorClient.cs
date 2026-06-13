using System.Net.Http.Json;
using RaceOps.Domain.Entities;
using RaceOps.Domain.Interfaces;
using RaceOps.Infrastructure.RaceMonitor.Responses;

namespace RaceOps.Infrastructure.RaceMonitor;

/// <summary>
/// Implementação do cliente HTTP para a API Race Monitor.
/// Todos os endpoints usam POST com apiToken no corpo da requisição.
/// Docs: https://api.race-monitor.com/v2/
/// </summary>
public class RaceMonitorClient(HttpClient http, string apiToken) : IRaceMonitorClient
{
    public async Task<IReadOnlyList<Race>> GetCurrentRacesAsync(CancellationToken ct = default)
    {
        var response = await PostAsync<CurrentRacesResponse>("Common/CurrentRaces",
            new { apiToken }, ct);

        return response.Races.Select(r => new Race
        {
            Id = r.ID,
            Name = r.Name,
            Track = r.Track,
            IsLive = r.IsLive,
            StartDateEpoch = r.StartDateEpoc,
            EndDateEpoch = r.EndDateEpoc,
            ImageUrl = r.ImageUrl ?? string.Empty
        }).ToList();
    }

    public async Task<LiveSession> GetSessionAsync(int raceId, CancellationToken ct = default)
    {
        var response = await PostAsync<GetSessionResponse>("Live/GetSession",
            new { apiToken, raceID = raceId }, ct);

        var s = response.Session;

        return new LiveSession
        {
            RunNumber = s.RunNumber,
            SessionName = s.SessionName,
            TrackName = s.TrackName,
            TrackLength = s.TrackLength,
            CurrentTime = s.CurrentTime,
            SessionTime = s.SessionTime,
            TimeToGo = s.TimeToGo,
            LapsToGo = s.LapsToGo,
            FlagStatus = ParseFlagStatus(s.FlagStatus),
            SortMode = s.SortMode,
            Classes = s.Classes?.ToDictionary(
                kv => kv.Key,
                kv => new RaceClass { ClassId = kv.Value.ClassID, Description = kv.Value.Description }
            ) ?? new Dictionary<string, RaceClass>(),
            Competitors = s.Competitors?.ToDictionary(
                kv => kv.Key,
                kv => MapCompetitor(kv.Value)
            ) ?? new Dictionary<string, Competitor>()
        };
    }

    public async Task<CompetitorDetail> GetRacerAsync(int raceId, string racerId, CancellationToken ct = default)
    {
        var response = await PostAsync<GetRacerResponse>("Live/GetRacer",
            new { apiToken, raceID = raceId, racerID = racerId }, ct);

        return new CompetitorDetail
        {
            Competitor = MapCompetitor(response.Details.Competitor),
            Laps = response.Details.Laps.Select(l => new LapTime
            {
                Lap = l.Lap,
                Position = l.Position,
                Time = l.LapTime,
                FlagStatus = l.FlagStatus,
                TotalTime = l.TotalTime
            }).ToList()
        };
    }

    public async Task<StreamingConnectionInfo> GetStreamingConnectionAsync(int raceId, CancellationToken ct = default)
    {
        var response = await PostAsync<GetStreamingConnectionResponse>("Live/GetStreamingConnection",
            new { apiToken, raceID = raceId }, ct);

        var info = response.ConnectionInfo;
        return new StreamingConnectionInfo
        {
            RaceId = info.RaceID,
            WebsocketUrl = info.WebsocketURL,
            LiveTimingToken = info.LiveTimingToken,
            Instance = info.Instance,
            IsLive = info.IsLive
        };
    }

    // --- helpers ---

    private async Task<T> PostAsync<T>(string endpoint, object body, CancellationToken ct)
    {
        var content = ToFormContent(body);
        var httpResponse = await http.PostAsync($"v2/{endpoint}", content, ct);
        httpResponse.EnsureSuccessStatusCode();

        var result = await httpResponse.Content.ReadFromJsonAsync<T>(ct)
            ?? throw new InvalidOperationException($"Resposta vazia de {endpoint}");

        return result;
    }

    private static FormUrlEncodedContent ToFormContent(object obj)
    {
        var props = obj.GetType().GetProperties()
            .Select(p => new KeyValuePair<string, string>(p.Name, p.GetValue(obj)?.ToString() ?? string.Empty));
        return new FormUrlEncodedContent(props);
    }

    private static Competitor MapCompetitor(CompetitorData c) => new()
    {
        RacerId = c.RacerID,
        Number = c.Number,
        FirstName = c.FirstName,
        LastName = c.LastName,
        Nationality = c.Nationality,
        ClassId = c.ClassID,
        Position = c.Position,
        Laps = c.Laps,
        TotalTime = c.TotalTime,
        BestLap = c.BestLap,
        BestLapTime = c.BestLapTime,
        LastLapTime = c.LastLapTime
    };

    private static FlagStatus ParseFlagStatus(string? value) => value?.Trim().ToLower() switch
    {
        "green" => FlagStatus.Green,
        "yellow" => FlagStatus.Yellow,
        "red" => FlagStatus.Red,
        "finish" => FlagStatus.Finish,
        _ => FlagStatus.Unknown
    };
}
