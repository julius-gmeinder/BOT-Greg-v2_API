using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Models.External;
using BOT_Greg_v2_API.Repos;

namespace BOT_Greg_v2_API.Services.Alerts;

public class MatchAlertsService
{
    private readonly AlertStreamService _stream;
    private readonly AlertsRepo _alerts;
    private readonly HistoryMatchAlertsRepo _history;
    private readonly AlertsHelper _helper;
    private readonly ILogger<MatchAlertsService> _logger;

    public MatchAlertsService(AlertStreamService stream, AlertsRepo alerts, HistoryMatchAlertsRepo history, AlertsHelper helper, ILogger<MatchAlertsService> logger)
    {
        _stream = stream;
        _alerts = alerts;
        _history = history;
        _helper = helper;
        _logger = logger;
    }

    public async Task SendMatchAlertsAsync(List<MatchUpcoming> matches, Dictionary<string, Team> teamsByName)
    {
        var startingMatches = await GetStartingMatchesAsync(matches);

        if (startingMatches.Count == 0)
            return;

        var guilds = await _alerts.GetMatchAlertGuildsAsync();
        List<MatchAlertDto> matchAlerts = new();

        foreach (var match in startingMatches)
        {
            var matchAlert = new MatchAlertDto()
            {
                Match = match
            };

            foreach (var guild in guilds)
            {
                var rolesToPing = _helper.GetRolesToPing(guild, match, teamsByName);

                if (rolesToPing == null)
                    continue;

                matchAlert.Guilds.Add(new MatchAlertDto.GuildData()
                {
                    ChannelId = guild.MatchAlertsChannelId!,
                    RoleIds = rolesToPing
                });
            }

            matchAlerts.Add(matchAlert);
        }

        var alertsToSend = matchAlerts.Where(x => x.Guilds.Count > 0).ToList();
        _logger.LogInformation($"Found {startingMatches.Count} starting matches, sending {alertsToSend.Count} match alerts");

        if (alertsToSend.Count > 0)
            _stream.Send(Constants.StreamEvents.MatchAlerts, alertsToSend);

        // matches without any alert are saved too, so every match only gets processed once
        await _history.AddBulkAsync(matchAlerts);
    }

    private async Task<List<MatchUpcoming>> GetStartingMatchesAsync(List<MatchUpcoming> matches)
    {
        // matches only contains matches that have not started yet, so the next sync interval is the alert window
        DateTime alertWindowEnd = DateTime.UtcNow.AddMinutes(Constants.SyncCooldownMinutes);

        var startingMatches = matches
            .Where(x => x.DateTime <= alertWindowEnd)
            .Where(x => x.TeamX != null && x.TeamY != null)
            .ToList();

        if (startingMatches.Count == 0)
            return startingMatches;

        var history = await _history.GetHistoryAsync(startingMatches.Select(x => x.MatchId));
        var alertedMatchIds = history.Select(x => x.MatchId).ToHashSet();

        return startingMatches.Where(x => !alertedMatchIds.Contains(x.MatchId)).ToList();
    }
}
