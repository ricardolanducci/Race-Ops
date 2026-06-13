using Moq;
using RaceOps.Application.UseCases;
using RaceOps.Domain.Entities;
using RaceOps.Domain.Interfaces;
using Xunit;

namespace RaceOps.Application.Tests.UseCases;

public class GetCurrentRacesUseCaseTests
{
    [Fact]
    public async Task Execute_ReturnsRaceDtos_MappedFromDomain()
    {
        var clientMock = new Mock<IRaceMonitorClient>();
        clientMock.Setup(c => c.GetCurrentRacesAsync(default))
            .ReturnsAsync([
                new Race { Id = 1, Name = "Race A", Track = "Track 1", IsLive = true },
                new Race { Id = 2, Name = "Race B", Track = "Track 2", IsLive = false }
            ]);

        var useCase = new GetCurrentRacesUseCase(clientMock.Object);
        var result = await useCase.ExecuteAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Race A", result[0].Name);
        Assert.True(result[0].IsLive);
        Assert.False(result[1].IsLive);
    }

    [Fact]
    public async Task Execute_ReturnsEmpty_WhenNoRaces()
    {
        var clientMock = new Mock<IRaceMonitorClient>();
        clientMock.Setup(c => c.GetCurrentRacesAsync(default))
            .ReturnsAsync([]);

        var useCase = new GetCurrentRacesUseCase(clientMock.Object);
        var result = await useCase.ExecuteAsync();

        Assert.Empty(result);
    }
}
