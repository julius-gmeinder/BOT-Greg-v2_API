using BOT_Greg_v2_API.Models.Entities;
using BOT_Greg_v2_API.Models.Enums;
using BOT_Greg_v2_API.Models.External;

namespace BOT_Greg_v2_API.Services;

// decides which matches / events fit the filters of a guild (used by the alerts and the schedule endpoints)
public class FilterService
{
    private readonly LiquipediaService _liquipedia;

    public FilterService(LiquipediaService liquipedia)
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

    public bool MatchPassesFilters(Guild guild, MatchBase match, Dictionary<string, Team> teamsByName)
    {
        return GetMatchingFilters(guild, match, teamsByName) != null;
    }

    // returns all filters the match passes, or null if the match passes none of them
    public List<Filter>? GetMatchingFilters(Guild guild, MatchBase match, Dictionary<string, Team> teamsByName)
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

    // events only know their name and tier, all other filter properties are ignored
    public bool EventPassesFilters(Guild guild, Event ev)
    {
        if (guild.Filters.Count == 0)
            return true;

        return guild.Filters.Any(filter => EventPassesFilter(ev, filter));
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

    private bool EventPassesFilter(Event ev, Filter filter)
    {
        if (filter.Event != null && !string.Equals(filter.Event, ev.EventName, StringComparison.OrdinalIgnoreCase))
            return false;

        // the tier of an event is not nullable, so a tier that is not part of the enum (e. g. misc events) has to be checked
        if (filter.EventTier != null && (!Enum.IsDefined(ev.Tier) || ev.Tier > filter.EventTier))
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
