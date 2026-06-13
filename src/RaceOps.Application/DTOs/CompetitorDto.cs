namespace RaceOps.Application.DTOs;

public record CompetitorDto(
    string Position,
    string Number,
    string FullName,
    string ClassId,
    string Laps,
    string BestLapTime,
    string LastLapTime,
    string TotalTime
);
