namespace RaceOps.Infrastructure.RaceMonitor;

/// <summary>
/// Parseia as mensagens do protocolo RMonitor recebidas via WebSocket do Race Monitor.
/// Cada mensagem é uma linha começando com o comando (ex: $F, $J, $G).
/// </summary>
public static class RMonitorParser
{
    public static RMonitorMessage Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return new RMonitorMessage { Command = string.Empty, Fields = [] };

        var parts = SplitLine(line);
        if (parts.Count == 0)
            return new RMonitorMessage { Command = string.Empty, Fields = [] };

        return new RMonitorMessage
        {
            Command = parts[0],
            Fields = parts.Skip(1).ToList()
        };
    }

    /// <summary>
    /// Divide a linha respeitando strings entre aspas.
    /// Ex: $F,0,"00:00:00","01:21:18","00:00:00","Green "
    /// </summary>
    private static List<string> SplitLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        result.Add(current.ToString().Trim());
        return result;
    }
}

public class RMonitorMessage
{
    public string Command { get; init; } = string.Empty;
    public List<string> Fields { get; init; } = [];

    public string Get(int index) => index < Fields.Count ? Fields[index] : string.Empty;
}
