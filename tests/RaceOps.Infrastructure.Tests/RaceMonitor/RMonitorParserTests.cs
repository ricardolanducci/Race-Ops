using RaceOps.Infrastructure.RaceMonitor;
using Xunit;

namespace RaceOps.Infrastructure.Tests.RaceMonitor;

public class RMonitorParserTests
{
    [Fact]
    public void Parse_Heartbeat_ExtractsAllFields()
    {
        var line = "$F,5,\"00:05:00\",\"01:21:18\",\"00:30:00\",\"Green \"";
        var msg = RMonitorParser.Parse(line);

        Assert.Equal("$F", msg.Command);
        Assert.Equal("5", msg.Get(0));
        Assert.Equal("00:05:00", msg.Get(1));
        Assert.Equal("01:21:18", msg.Get(2));
        Assert.Equal("00:30:00", msg.Get(3));
        // O parser normaliza o padding que a cronometragem envia ("Green ")
        Assert.Equal("Green", msg.Get(4));
    }

    [Fact]
    public void Parse_CompetitorInfo_ExtractsAllFields()
    {
        var line = "$A,\"1001\",\"44\",\"T123\",\"Lewis\",\"Hamilton\",\"UK\",\"1\"";
        var msg = RMonitorParser.Parse(line);

        Assert.Equal("$A", msg.Command);
        Assert.Equal("1001", msg.Get(0));
        Assert.Equal("44", msg.Get(1));
        Assert.Equal("T123", msg.Get(2));
        Assert.Equal("Lewis", msg.Get(3));
        Assert.Equal("Hamilton", msg.Get(4));
        Assert.Equal("UK", msg.Get(5));
        Assert.Equal("1", msg.Get(6));
    }

    [Fact]
    public void Parse_PassingInfo_ExtractsRacerIdAndTimes()
    {
        var line = "$J,\"1001\",\"01:22.345\",\"00:45:12.345\"";
        var msg = RMonitorParser.Parse(line);

        Assert.Equal("$J", msg.Command);
        Assert.Equal("1001", msg.Get(0));
        Assert.Equal("01:22.345", msg.Get(1));
        Assert.Equal("00:45:12.345", msg.Get(2));
    }

    [Fact]
    public void Parse_EmptyLine_ReturnsEmptyCommand()
    {
        var msg = RMonitorParser.Parse(string.Empty);
        Assert.Equal(string.Empty, msg.Command);
    }

    [Fact]
    public void Parse_ResetCommand_ReturnsICommand()
    {
        var line = "$I,\"12:00:00.000\",\"31 May 26\"";
        var msg = RMonitorParser.Parse(line);

        Assert.Equal("$I", msg.Command);
    }

    [Theory]
    [InlineData("$RMS,\"race\"", "$RMS", "race")]
    [InlineData("$RMS,\"qualifying\"", "$RMS", "qualifying")]
    public void Parse_SortMode_ExtractsModeCorrectly(string line, string expectedCmd, string expectedMode)
    {
        var msg = RMonitorParser.Parse(line);
        Assert.Equal(expectedCmd, msg.Command);
        Assert.Equal(expectedMode, msg.Get(0));
    }
}
