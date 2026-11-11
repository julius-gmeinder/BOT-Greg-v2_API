namespace BOT_Greg_v2_API.Services.Alerts;

// alerts can be minutes or hours apart, the heartbeat keeps the connection from idling out
// and lets the client notice a dead connection
public class AlertStreamHeartbeat : BackgroundService
{
    private readonly AlertStreamService _stream;

    public AlertStreamHeartbeat(AlertStreamService stream)
    {
        _stream = stream;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Constants.StreamHeartbeatSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
            _stream.Send(Constants.StreamEvents.Heartbeat, DateTime.UtcNow);
    }
}
