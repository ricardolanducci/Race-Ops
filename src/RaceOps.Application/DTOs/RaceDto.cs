namespace RaceOps.Application.DTOs;

public record RaceDto(
    int Id,
    string Name,
    string Track,
    bool IsLive,
    long StartDateEpoch,
    string ImageUrl
);
