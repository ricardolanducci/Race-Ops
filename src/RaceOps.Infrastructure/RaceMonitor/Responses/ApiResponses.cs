using System.Text.Json.Serialization;

namespace RaceOps.Infrastructure.RaceMonitor.Responses;

// --- /v2/Common/CurrentRaces ---
public record CurrentRacesResponse(bool Successful, List<RaceData> Races);

public record RaceData(
    int ID,
    string Name,
    string Track,
    bool IsLive,
    long StartDateEpoc,
    long EndDateEpoc,
    string? ImageUrl
);

// --- /v2/Live/GetSession ---
public record GetSessionResponse(bool Successful, SessionData Session);

public record SessionData(
    string RunNumber,
    string SessionName,
    string TrackName,
    string TrackLength,
    string CurrentTime,
    string SessionTime,
    string TimeToGo,
    string LapsToGo,
    string FlagStatus,
    string SortMode,
    Dictionary<string, ClassData>? Classes,
    Dictionary<string, CompetitorData>? Competitors
);

public record ClassData(string ClassID, string Description);

public record CompetitorData(
    string RacerID,
    string Number,
    string FirstName,
    string LastName,
    string Nationality,
    string ClassID,
    string Position,
    string Laps,
    string TotalTime,
    string BestLap,
    string BestLapTime,
    string LastLapTime,
    string Transponder = "",
    string AdditionalData = "",
    string BestPosition = ""
);

// --- /v2/Live/GetRacer ---
public record GetRacerResponse(bool Successful, RacerDetails Details);

public record RacerDetails(CompetitorData Competitor, List<LapData> Laps);

public record LapData(string Lap, string Position, string LapTime, string FlagStatus, string TotalTime);

// --- /v2/Live/GetStreamingConnection ---
public record GetStreamingConnectionResponse(bool Successful, ConnectionInfo ConnectionInfo);

public record ConnectionInfo(
    int RaceID,
    string WebsocketURL,
    string LiveTimingToken,
    int Instance,
    bool IsLive,
    string? WebsiteRestrictions
);
