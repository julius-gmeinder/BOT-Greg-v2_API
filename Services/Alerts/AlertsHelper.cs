using BOT_Greg_v2_API.Models.Entities;
using BOT_Greg_v2_API.Models.External;

namespace BOT_Greg_v2_API.Services.Alerts;

public class AlertsHelper
{
    private readonly LiquipediaService _liquipedia;
    private readonly FilterService _filters;

    public AlertsHelper(LiquipediaService liquipedia, FilterService filters)
    {
        _liquipedia = liquipedia;
        _filters = filters;
    }

    public async Task<MatchUpcoming?> GetGuildsNextMatchAsync(Guild guild)
    {
        var matches = await _liquipedia.GetMatchesAsync();
        var teamsByName = await _filters.GetTeamsByNameAsync();

        return matches.FirstOrDefault(match => _filters.MatchPassesFilters(guild, match, teamsByName));
    }

    // returns null if the guild should not be alerted about the match
    // returns an empty list if the guild should be alerted, but its role management is disabled (alert without mentions)
    public List<string>? GetRolesToPing(Guild guild, MatchBase match, Dictionary<string, Team> teamsByName)
    {
        var matchingFilters = _filters.GetMatchingFilters(guild, match, teamsByName);

        if (matchingFilters == null)
            return null;

        List<string> rolesToPing = new();

        if (!guild.Settings.RoleManagement)
            return rolesToPing;

        if (guild.AlertRoleDiscordId != null)
            rolesToPing.Add(guild.AlertRoleDiscordId);

        foreach (var filter in matchingFilters)
        {
            if (filter.RoleDiscordId != null)
                rolesToPing.Add(filter.RoleDiscordId);
        }

        var linkedRoleDiscordIds = guild.RoleLinks
            .Where(link => rolesToPing.Contains(link.BotRoleDiscordId))
            .Select(link => link.LinkedRoleDiscordId)
            .ToList();

        rolesToPing.AddRange(linkedRoleDiscordIds);

        return rolesToPing.Distinct().ToList();
    }
}
