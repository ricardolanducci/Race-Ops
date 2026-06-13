using RaceOps.Domain.Entities;

namespace RaceOps.Domain.Interfaces;

public interface IEventConfigRepository
{
    Task<EventConfig?> GetAsync(int raceId, CancellationToken ct = default);
    Task SaveAsync(EventConfig config, CancellationToken ct = default);
}

public interface IStintRepository
{
    Task<IReadOnlyList<Stint>> GetByKartAsync(int raceId, string kartNumber, CancellationToken ct = default);
    Task<IReadOnlyList<Stint>> GetByRaceAsync(int raceId, CancellationToken ct = default);
    Task SaveAsync(Stint stint, CancellationToken ct = default);
    Task DeleteAsync(int stintId, CancellationToken ct = default);
}
