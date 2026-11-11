using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Models.External;
using BOT_Greg_v2_API.Repos;

namespace BOT_Greg_v2_API.Services.Alerts;

public class ResultAlertsService
{
    private readonly AlertStreamService _stream;
    private readonly AlertsRepo _alerts;
    private readonly HistoryResultAlertsRepo _history;
    private readonly AlertsHelper _helper;
    private readonly ILogger<ResultAlertsService> _logger;

    public ResultAlertsService(AlertStreamService stream, AlertsRepo alerts, HistoryResultAlertsRepo history, AlertsHelper helper, ILogger<ResultAlertsService> logger)
    {
        _stream = stream;
        _alerts = alerts;
        _history = history;
        _helper = helper;
        _logger = logger;
    }

    public async Task SendResultAlertsAsync(List<MatchResult> results, Dictionary<string, Team> teamsByName)
    {
        var newResults = await GetNewResultsAsync(results);

        if (newResults.Count == 0)
            return;

        var guilds = await _alerts.GetResultAlertGuildsAsync();
        List<ResultAlertDto> resultAlerts = new();

        foreach (var result in newResults)
        {
            var resultAlert = new ResultAlertDto()
            {
                Result = result
            };

            foreach (var guild in guilds)
            {
                var rolesToPing = _helper.GetRolesToPing(guild, result, teamsByName);

                if (rolesToPing == null)
                    continue;

                resultAlert.Guilds.Add(new ResultAlertDto.GuildData()
                {
                    ChannelDiscordId = guild.ResultAlertsChannelDiscordId!,
                    MentionOnResults = guild.Settings.MentionOnResults,
                    CensorResults = guild.Settings.CensorResults,
                    RoleDiscordIds = rolesToPing
                });
            }

            resultAlerts.Add(resultAlert);
        }

        var alertsToSend = resultAlerts.Where(x => x.Guilds.Count > 0).ToList();
        _logger.LogInformation($"Found {newResults.Count} new results, sending {alertsToSend.Count} result alerts");

        if (alertsToSend.Count > 0)
            _stream.Send(Constants.StreamEvents.ResultAlerts, alertsToSend);

        // results without any alert are saved too, so every result only gets processed once
        await _history.AddBulkAsync(resultAlerts);
    }

    private async Task<List<MatchResult>> GetNewResultsAsync(List<MatchResult> results)
    {
        var finishedMatches = results
            .Where(x => x.TeamX != null && x.TeamY != null)
            .OrderBy(x => x.DateTime)
            .ToList();

        if (finishedMatches.Count == 0)
            return finishedMatches;

        var history = await _history.GetHistoryAsync(finishedMatches.Select(x => x.MatchId));
        var alertedMatchIds = history.Select(x => x.MatchId).ToHashSet();

        return finishedMatches.Where(x => !alertedMatchIds.Contains(x.MatchId)).ToList();
    }
}
