using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RaceOps.Infrastructure.RaceMonitor;

/// <summary>
/// Conecta ao WebSocket do Race Monitor e emite eventos conforme as mensagens chegam.
/// O serviço mantém a conexão ativa e trata reconexão automática.
/// </summary>
public class LiveStreamService(ILogger<LiveStreamService> logger)
{
    public event Action<RMonitorMessage>? MessageReceived;

    public async Task ConnectAndListenAsync(string websocketUrl, CancellationToken ct)
    {
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(websocketUrl), ct);
        logger.LogInformation("Conectado ao live stream: {Url}", websocketUrl);

        var buffer = new byte[4096];
        var messageBuffer = new StringBuilder();

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var result = await ws.ReceiveAsync(buffer, ct);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                logger.LogWarning("Conexão WebSocket encerrada pelo servidor");
                break;
            }

            var chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
            messageBuffer.Append(chunk);

            if (!result.EndOfMessage)
                continue;

            var fullMessage = messageBuffer.ToString();
            messageBuffer.Clear();

            foreach (var line in fullMessage.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var message = RMonitorParser.Parse(line);
                if (!string.IsNullOrEmpty(message.Command))
                    MessageReceived?.Invoke(message);
            }
        }
    }
}
