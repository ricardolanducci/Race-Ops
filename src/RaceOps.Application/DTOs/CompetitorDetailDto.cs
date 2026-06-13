namespace RaceOps.Application.DTOs;

public record CompetitorDetailDto(
    CompetitorDto Competitor,
    IReadOnlyList<LapTimeDto> Laps
);

public record LapTimeDto(
    string Lap,
    string Position,
    string Time,
    string FlagStatus,
    string TotalTime
);
