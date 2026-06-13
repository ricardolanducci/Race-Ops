using Moq;
using RaceOps.Application.UseCases;
using RaceOps.Domain.Entities;
using RaceOps.Domain.Interfaces;
using Xunit;

namespace RaceOps.Application.Tests.UseCases;

public class GetLiveSessionUseCaseTests
{
    private readonly Mock<IRaceMonitorClient> _clientMock = new();

    [Fact]
    public async Task Execute_ReturnsCompetitorsOrderedByPosition()
    {
        _clientMock.Setup(c => c.GetSessionAsync(123, default))
            .ReturnsAsync(BuildSession(
                ("P3", "Piloto C"),
                ("P1", "Piloto A"),
                ("P2", "Piloto B")
            ));

        var useCase = new GetLiveSessionUseCase(_clientMock.Object);
        var result = await useCase.ExecuteAsync(123);

        Assert.Equal(3, result.Competitors.Count);
        Assert.Equal("1", result.Competitors[0].Position);
        Assert.Equal("2", result.Competitors[1].Position);
        Assert.Equal("3", result.Competitors[2].Position);
    }

    [Fact]
    public async Task Execute_MapsFlagStatusToString()
    {
        _clientMock.Setup(c => c.GetSessionAsync(42, default))
            .ReturnsAsync(new LiveSession { FlagStatus = FlagStatus.Yellow });

        var useCase = new GetLiveSessionUseCase(_clientMock.Object);
        var result = await useCase.ExecuteAsync(42);

        Assert.Equal("Yellow", result.FlagStatus);
    }

    [Fact]
    public async Task Execute_ReturnsEmptyListWhenNoCompetitors()
    {
        _clientMock.Setup(c => c.GetSessionAsync(1, default))
            .ReturnsAsync(new LiveSession());

        var useCase = new GetLiveSessionUseCase(_clientMock.Object);
        var result = await useCase.ExecuteAsync(1);

        Assert.Empty(result.Competitors);
    }

    // --- helpers ---

    private static LiveSession BuildSession(params (string Position, string Name)[] entries)
    {
        var competitors = new Dictionary<string, Competitor>();
        foreach (var (pos, name) in entries)
        {
            var parts = name.Split(' ');
            var racer = new Competitor
            {
                RacerId = pos,
                Position = pos.Replace("P", ""),
                FirstName = parts[0],
                LastName = parts.Length > 1 ? parts[1] : string.Empty
            };
            competitors[pos] = racer;
        }
        return new LiveSession { Competitors = competitors };
    }
}
