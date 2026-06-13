using RaceOps.Application.UseCases;
using RaceOps.Domain.Entities;
using Xunit;

namespace RaceOps.Application.Tests.UseCases;

public class StintBuilderTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    private static EventConfig Config() => new()
    {
        RaceId = 1,
        RaceDurationMinutes = 120,
        PitRules = [new PitRule { Name = "06m", MinStopSeconds = 360, RequiredCount = 3 }]
    };

    private static KartLiveState KartWithStop()
    {
        var kart = new KartLiveState
        {
            RacerId = "14", Number = "014", DriverName = "Juliano",
            CurrentLap = 5, LastPassAt = Now.AddSeconds(-30)
        };
        kart.Laps.Add(new LapRecord(1, 60, Now.AddMinutes(-20)));
        kart.Laps.Add(new LapRecord(2, 61, Now.AddMinutes(-19)));
        kart.Laps.Add(new LapRecord(3, 400, Now.AddMinutes(-12))); // parada
        kart.Laps.Add(new LapRecord(4, 59, Now.AddMinutes(-11)));
        kart.Laps.Add(new LapRecord(5, 60, Now.AddMinutes(-10)));
        return kart;
    }

    [Fact]
    public void Build_DetectsStopAndSplitsStints()
    {
        var stints = StintBuilder.Build(KartWithStop(), Config(), [], Now);

        Assert.Equal(2, stints.Count);

        var first = stints[0];
        Assert.Equal(1, first.Number);
        Assert.Equal(2, first.ExitLap);          // volta de saída = volta da parada - 1
        Assert.Equal(400, first.StopTimeSeconds);
        Assert.False(first.IsCurrent);
        Assert.Equal(121, first.TrackTimeSeconds!.Value, 1);

        var current = stints[1];
        Assert.Equal(2, current.Number);
        Assert.Equal(3, current.EntryLap);       // volta de entrada = volta da parada
        Assert.True(current.IsCurrent);
        Assert.Equal("Juliano", current.Driver);
        // 59 + 60 + 30s correndo desde a última passagem
        Assert.Equal(149, current.TrackTimeSeconds!.Value, 0);
    }

    [Fact]
    public void Build_EstimatedStintFromRaceDurationAndStops()
    {
        var stints = StintBuilder.Build(KartWithStop(), Config(), [], Now);

        // 120 min / (3 paradas + 1) = 30 min
        Assert.All(stints, s => Assert.Equal(1800, s.EstimatedSeconds!.Value, 1));
    }

    [Fact]
    public void Build_ManualEditOverridesAutoStint()
    {
        var manual = new Stint
        {
            Id = 7, RaceId = 1, KartNumber = "014", Number = 1,
            Driver = "Rafael", Heavy = true, IsManual = true
        };

        var stints = StintBuilder.Build(KartWithStop(), Config(), [manual], Now);

        var first = stints[0];
        Assert.Equal("Rafael", first.Driver);
        Assert.True(first.Heavy);
        Assert.True(first.IsManual);
        Assert.Equal(2, first.ExitLap); // detecção automática preservada
    }

    [Fact]
    public void Build_ExtraManualStintAppended()
    {
        var planned = new Stint
        {
            RaceId = 1, KartNumber = "014", Number = 5, Driver = "Lucas", IsManual = true
        };

        var stints = StintBuilder.Build(KartWithStop(), Config(), [planned], Now);

        Assert.Equal(3, stints.Count);
        Assert.Equal("Lucas", stints[^1].Driver);
        Assert.Equal(5, stints[^1].Number);
    }
}
