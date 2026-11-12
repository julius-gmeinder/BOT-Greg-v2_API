using BOT_Greg_v2_API.Models.Entities;
using BOT_Greg_v2_API.Models.External;

namespace BOT_Greg_v2_API.Services.Alerts;

public class AlertsHelper
{
    private readonly LiquipediaService _liquipedia;

    public AlertsHelper(LiquipediaService liquipedia)
    {
        _liquipedia = liquipedia;
    }

    // teams are needed for the vrs rank filter, the match itself only knows the team names
    public async Task<Dictionary<string, Team>> GetTeamsByNameAsync()
    {
        Dictionary<string, Team> teamsByName = new(StringComparer.OrdinalIgnoreCase);

        foreach (var team in await _liquipedia.GetTeamsAsync())
            teamsByName.TryAdd(team.Name, team);

        return teamsByName;
    }

    public async Task<MatchUpcoming?> GetGuildsNextMatchAsync(Guild guild)
    {
        var matches = await _liquipedia.GetMatchesAsync();
        var teamsByName = await GetTeamsByNameAsync();

        return matches.FirstOrDefault(match => GetMatchingFilters(guild, match, teamsByName) != null);
    }

    // returns null if the guild should not be alerted about the match
    // returns an empty list if the guild should be alerted, but its role management is disabled (alert without mentions)
    public List<string>? GetRolesToPing(Guild guild, MatchBase match, Dictionary<string, Team> teamsByName)
    {
        var matchingFilters = GetMatchingFilters(guild, match, teamsByName);

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

    // returns all filters the match passes, or null if the match passes none of them
    private List<Filter>? GetMatchingFilters(Guild guild, MatchBase match, Dictionary<string, Team> teamsByName)
    {
        // a guild without filters gets every match
        // (new guilds start with the default filter, so this only applies to guilds that deleted all of their filters)
        if (guild.Filters.Count == 0)
            return new List<Filter>();

        var matchingFilters = guild.Filters
            .Where(filter => MatchPassesFilter(match, filter, teamsByName))
            .ToList();

        return matchingFilters.Count > 0 ? matchingFilters : null;
    }

    // a filter property that is null means "dont care", all other properties have to fit the match
    private bool MatchPassesFilter(MatchBase match, Filter filter, Dictionary<string, Team> teamsByName)
    {
        if (filter.Team != null && !IsTeam(match.TeamX, filter.Team) && !IsTeam(match.TeamY, filter.Team))
            return false;

        if (filter.Event != null && !string.Equals(filter.Event, match.EventName, StringComparison.OrdinalIgnoreCase))
            return false;

        // lower value = higher tier, the match has to be in the filters tier or above
        if (filter.EventTier != null && (match.Tier == null || match.Tier > filter.EventTier))
            return false;

        if (filter.VrsRank != null && (!IsRankedWithin(match.TeamX, filter.VrsRank.Value, teamsByName) || !IsRankedWithin(match.TeamY, filter.VrsRank.Value, teamsByName)))
            return false;

        if (filter.HltvCoverage != null && filter.HltvCoverage != match.IsHltvCovered)
            return false;

        return true;
    }

    private bool IsTeam(MatchTeam? matchTeam, string teamName)
    {
        return string.Equals(matchTeam?.Name, teamName, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsRankedWithin(MatchTeam? matchTeam, int vrsRank, Dictionary<string, Team> teamsByName)
    {
        if (matchTeam == null)
            return false;

        if (!teamsByName.TryGetValue(matchTeam.Name, out var team) || team.Vrs == null)
            return false;

        return team.Vrs.Rank <= vrsRank;
    }
}
