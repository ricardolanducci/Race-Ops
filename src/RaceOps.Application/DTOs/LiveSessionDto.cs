namespace RaceOps.Application.DTOs;

public record LiveSessionDto(
    string SessionName,
    string TrackName,
    string FlagStatus,
    string TimeToGo,
    string LapsToGo,
    string SessionTime,
    string SortMode,
    IReadOnlyList<ClassDto> Classes,
    IReadOnlyList<CompetitorDto> Competitors
);

public record ClassDto(string ClassId, string Description);
