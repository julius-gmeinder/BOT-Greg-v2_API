using BOT_Greg_v2_API.Services.Alerts;

namespace BOT_Greg_v2_API.Services;

public class BackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly AlertStreamService _stream;
    private readonly ILogger<BackgroundWorker> _logger;

    public BackgroundWorker(IServiceProvider services, AlertStreamService stream, ILogger<BackgroundWorker> logger)
    {
        _services = services;
        _stream = stream;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan cooldown = TimeSpan.FromMinutes(Constants.SyncCooldownMinutes);

        // the first run waits for the next full interval (e. g. xx:00, xx:05, xx:10), because matches start at those times
        TimeSpan timeToWait = cooldown - TimeSpan.FromTicks(DateTime.UtcNow.Ticks % cooldown.Ticks);

        _logger.LogInformation($"Waiting {timeToWait:mm\\:ss} minutes to sync the timer with the clock");
        await Task.Delay(timeToWait, stoppingToken);

        _logger.LogInformation($"Starting timer with {Constants.SyncCooldownMinutes} minute intervals");
        using var timer = new PeriodicTimer(cooldown);

        await DoWorkAsync();

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await DoWorkAsync();
    }

    private async Task DoWorkAsync()
    {
        try
        {
            using var scope = _services.CreateScope();

            var liquipedia = scope.ServiceProvider.GetRequiredService<LiquipediaService>();
            var alertsHelper = scope.ServiceProvider.GetRequiredService<AlertsHelper>();

            var matchDelaysService = scope.ServiceProvider.GetRequiredService<MatchDelaysService>();
            var matchAlertsService = scope.ServiceProvider.GetRequiredService<MatchAlertsService>();
            var resultAlertsService = scope.ServiceProvider.GetRequiredService<ResultAlertsService>();

            // alerts need the newest data, so matches and results skip the cache
            var matches = await liquipedia.GetMatchesAsync(refetch: true);
            var results = await liquipedia.GetResultsAsync(refetch: true);
            var teamsByName = await alertsHelper.GetTeamsByNameAsync();

            if (_stream.ClientCount == 0)
                _logger.LogWarning("No client is connected to the alert stream, alerts of this run will not be received");

            // delays have to run first, otherwise the matches alerted in this run would be compared to themselves
            await matchDelaysService.SendMatchDelaysAsync(matches, teamsByName);
            await matchAlertsService.SendMatchAlertsAsync(matches, teamsByName);
            await resultAlertsService.SendResultAlertsAsync(results, teamsByName);
        }
        catch (Exception ex)
        {
            // an unhandled exception would stop the worker (and the app) for good, the next run just tries again
            _logger.LogError(ex, "Sending alerts failed");
        }
    }
}
