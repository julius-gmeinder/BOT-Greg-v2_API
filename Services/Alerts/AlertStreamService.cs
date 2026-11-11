using System.Collections.Concurrent;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

namespace BOT_Greg_v2_API.Services.Alerts;

// replaces the signalr hub: clients subscribe via server-sent-events and only receive, they never send
public class AlertStreamService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<AlertStreamService> _logger;

    // every connected client has its own channel, that buffers the events until they are written to the response
    private readonly ConcurrentDictionary<Guid, Channel<SseItem<string>>> _clients = new();

    // same format as the json responses of the controllers (camelCase)
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AlertStreamService(IHostApplicationLifetime lifetime, ILogger<AlertStreamService> logger)
    {
        _lifetime = lifetime;
        _logger = logger;
    }

    public int ClientCount => _clients.Count;

    public async IAsyncEnumerable<SseItem<string>> SubscribeAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Guid clientId = Guid.CreateVersion7();
        var channel = Channel.CreateUnbounded<SseItem<string>>();

        // completing the channel ends the loop below, so the stream closes when the client disconnects or the app shuts down
        using var clientDisconnected = cancellationToken.Register(() => channel.Writer.TryComplete());
        using var appStopping = _lifetime.ApplicationStopping.Register(() => channel.Writer.TryComplete());

        _clients[clientId] = channel;
        _logger.LogInformation($"Client connected to alert stream, {ClientCount} connected in total");

        try
        {
            await foreach (var sseItem in channel.Reader.ReadAllAsync())
                yield return sseItem;
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            _logger.LogInformation($"Client disconnected from alert stream, {ClientCount} connected in total");
        }
    }

    public void Send<T>(string eventType, T data)
    {
        // serialized once for all clients
        string json = JsonSerializer.Serialize(data, JsonOptions);
        SseItem<string> sseItem = new(json, eventType);

        foreach (var channel in _clients.Values)
            channel.Writer.TryWrite(sseItem);
    }
}
