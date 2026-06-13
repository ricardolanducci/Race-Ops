using RaceOps.Domain.Entities;
using RaceOps.Domain.Services;
using Xunit;

namespace RaceOps.Application.Tests.Services;

public class StandingsCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    private static EventConfig Config() => new()
    {
        RaceId = 1,
        RaceDurationMinutes = 720,
        PitRules =
        [
            new PitRule { Name = "06m", MinStopSeconds = 360, RequiredCount = 9 },
            new PitRule { Name = "10m", MinStopSeconds = 600, RequiredCount = 1 }
        ],
        TrackTimeAlertMinutes = 50,
        AttentionLapSeconds = 120,
        BoxLapSeconds = 180
    };

    private static KartLiveState Kart(string number, int position, params double[] laps)
    {
        var kart = new KartLiveState
        {
            RacerId = number,
            Number = number,
            TimingPosition = position,
            CurrentLap = laps.Length,
            LastLapSeconds = laps.Length > 0 ? laps[^1] : 0,
            TotalTimeSeconds = laps.Sum(),
            LastPassAt = Now.AddSeconds(-30)
        };
        for (var i = 0; i < laps.Length; i++)
            kart.Laps.Add(new LapRecord(i + 1, laps[i], Now.AddMinutes(-laps.Length + i)));
        return kart;
    }

    [Fact]
    public void Avg5_UsesOnlyLastFiveCleanLaps()
    {
        // 6 voltas limpas + 1 parada no meio — a parada não entra na média
        var kart = Kart("14", 1, 60, 61, 400, 58, 59, 60, 61, 62);

        var row = StandingsCalculator.Compute([kart], Config(), Now).Single();

        Assert.NotNull(row.Avg5Seconds);
        Assert.Equal(60.0, row.Avg5Seconds!.Value, 3); // (58+59+60+61+62)/5
    }

    [Fact]
    public void StopsPerRule_CountsTowardLargestSatisfiedRule()
    {
        // Uma parada de 6min (400s) e uma de 10min (650s)
        var kart = Kart("14", 1, 60, 400, 60, 650, 60);

        var row = StandingsCalculator.Compute([kart], Config(), Now).Single();

        Assert.Equal(1, row.StopsPerRule[0]); // regra 06m
        Assert.Equal(1, row.StopsPerRule[1]); // regra 10m
    }

    [Fact]
    public void StintAvg_ConsidersOnlyLapsAfterLastStop()
    {
        var kart = Kart("14", 1, 70, 71, 400, 58, 60);

        var row = StandingsCalculator.Compute([kart], Config(), Now).Single();

        Assert.Equal(2, row.StintLapCount);
        Assert.Equal(59.0, row.StintAvgSeconds!.Value, 3);
        Assert.Equal(58.0, row.BestStintLapSeconds!.Value, 3);
    }

    [Fact]
    public void RealPosition_FavorsKartWithFewerRemainingStops()
    {
        // Kart A líder na pista mas sem paradas; kart B uma volta atrás com 1 parada feita
        var kartA = Kart("1", 1, 60, 60, 60, 60);
        var kartB = Kart("2", 2, 60, 400, 60);

        var rows = StandingsCalculator.Compute([kartA, kartB], Config(), Now);

        Assert.Equal("2", rows.First(r => r.RealPosition == 1).Kart);
        Assert.Equal("1", rows.First(r => r.RealPosition == 2).Kart);
    }

    [Fact]
    public void LastLapRank_OrdersAscending()
    {
        var kartA = Kart("1", 1, 60, 59);
        var kartB = Kart("2", 2, 60, 58);
        var kartC = Kart("3", 3, 60, 61);

        var rows = StandingsCalculator.Compute([kartA, kartB, kartC], Config(), Now);

        Assert.Equal(1, rows.First(r => r.Kart == "2").LastLapRank);
        Assert.Equal(2, rows.First(r => r.Kart == "1").LastLapRank);
        Assert.Equal(3, rows.First(r => r.Kart == "3").LastLapRank);
    }

    [Fact]
    public void Status_BoxWhenRunningLapExceedsBoxThreshold()
    {
        var kart = Kart("14", 1, 60, 60);
        kart.LastPassAt = Now.AddSeconds(-200); // parado há 200s

        var row = StandingsCalculator.Compute([kart], Config(), Now).Single();

        Assert.Equal(KartStatus.Box, row.Status);
    }

    [Fact]
    public void Status_NearStopWhenTrackTimeExceedsAlert()
    {
        // 51 voltas de 60s = 51 minutos em pista, acima do alerta de 50min
        var laps = Enumerable.Repeat(60.0, 51).ToArray();
        var kart = Kart("14", 1, laps);

        var row = StandingsCalculator.Compute([kart], Config(), Now).Single();

        Assert.Equal(KartStatus.NearStop, row.Status);
    }

    [Fact]
    public void Gap_SameLapUsesTotalTimeDifference()
    {
        var kartA = Kart("1", 1, 60, 400, 60, 60); // total 580
        var kartB = Kart("2", 2, 61, 401, 61, 62); // total 585, mesmas paradas/voltas

        var rows = StandingsCalculator.Compute([kartA, kartB], Config(), Now);
        var second = rows.Single(r => r.RealPosition == 2);

        Assert.Equal(5.0, second.GapSeconds!.Value, 3);
        Assert.Null(second.GapLaps);
    }

    [Fact]
    public void Gap_FewerLapsReportedInLaps()
    {
        var kartA = Kart("1", 1, 60, 400, 60, 60, 60);
        var kartB = Kart("2", 2, 60, 400, 60);

        var rows = StandingsCalculator.Compute([kartA, kartB], Config(), Now);
        var second = rows.Single(r => r.RealPosition == 2);

        Assert.Equal(2, second.GapLaps);
    }
}
