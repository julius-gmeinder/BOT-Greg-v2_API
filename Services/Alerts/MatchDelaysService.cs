using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Models.External;
using BOT_Greg_v2_API.Repos;

namespace BOT_Greg_v2_API.Services.Alerts;

public class MatchDelaysService
{
    private readonly AlertStreamService _stream;
    private readonly AlertsRepo _alerts;
    private readonly HistoryMatchAlertsRepo _history;
    private readonly AlertsHelper _helper;
    private readonly ILogger<MatchDelaysService> _logger;

    public MatchDelaysService(AlertStreamService stream, AlertsRepo alerts, HistoryMatchAlertsRepo history, AlertsHelper helper, ILogger<MatchDelaysService> logger)
    {
        _stream = stream;
        _alerts = alerts;
        _history = history;
        _helper = helper;
        _logger = logger;
    }

    public async Task SendMatchDelaysAsync(List<MatchUpcoming> matches, Dictionary<string, Team> teamsByName)
    {
        var delayedMatches = await GetDelayedMatchesAsync(matches);

        if (delayedMatches.Count == 0)
            return;

        var guilds = await _alerts.GetMatchAlertGuildsAsync();
        List<MatchDelayDto> matchDelays = new();

        foreach (var match in delayedMatches)
        {
            var matchDelay = new MatchDelayDto()
            {
                Match = match
            };

            foreach (var guild in guilds)
            {
                var rolesToPing = _helper.GetRolesToPing(guild, match, teamsByName);

                if (rolesToPing == null)
                    continue;

                matchDelay.Guilds.Add(new MatchDelayDto.GuildData()
                {
                    ChannelDiscordId = guild.MatchAlertsChannelDiscordId!,
                    MentionOnDelays = guild.Settings.MentionOnDelays,
                    RoleDiscordIds = rolesToPing
                });
            }

            if (matchDelay.Guilds.Count > 0)
                matchDelays.Add(matchDelay);
        }

        _logger.LogInformation($"Found {delayedMatches.Count} delayed matches, sending {matchDelays.Count} match delays");

        if (matchDelays.Count > 0)
            _stream.Send(Constants.StreamEvents.MatchDelays, matchDelays);

        // saves the new start times that were set in the history
        await _history.SaveChangesAsync();
    }

    // a match that was already alerted but is upcoming again with a different start time got delayed
    private async Task<List<MatchUpcoming>> GetDelayedMatchesAsync(List<MatchUpcoming> matches)
    {
        List<MatchUpcoming> delayedMatches = new();

        if (matches.Count == 0)
            return delayedMatches;

        var history = await _history.GetHistoryAsync(matches.Select(x => x.MatchId));
        var historyByMatchId = history.ToDictionary(x => x.MatchId);

        foreach (var match in matches)
        {
            if (!historyByMatchId.TryGetValue(match.MatchId, out var matchInHistory))
                continue;

            if (matchInHistory.DateTime == match.DateTime)
                continue;

            matchInHistory.DateTime = match.DateTime;
            delayedMatches.Add(match);
        }

        return delayedMatches;
    }
}
