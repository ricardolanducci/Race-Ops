namespace RaceOps.Infrastructure.Live;

/// <summary>
/// Grava cada linha recebida do WebSocket RMonitor em formato
/// {unix_ms}|{linha}, uma por linha. Use Replay.cs para reproduzir depois.
/// </summary>
internal sealed class StreamRecorder : IDisposable
{
    private readonly StreamWriter _writer;
    public string FilePath { get; }

    private StreamRecorder(string path)
    {
        FilePath = path;
        _writer = new StreamWriter(path, append: false, System.Text.Encoding.UTF8) { AutoFlush = false };
    }

    public static StreamRecorder? Create(int raceId, string dir = "recordings")
    {
        try
        {
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"race_{raceId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.rmon");
            return new StreamRecorder(path);
        }
        catch
        {
            return null;
        }
    }

    public void Record(string line) =>
        _writer.WriteLine($"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}|{line}");

    public void Flush() => _writer.Flush();

    public void Dispose()
    {
        _writer.Flush();
        _writer.Dispose();
    }
}
