using BOT_Greg_v2_API.Models.Entities;
using BOT_Greg_v2_API.Models.External;
using BOT_Greg_v2_API.Repos;
using BOT_Greg_v2_API.Services;
using BOT_Greg_v2_API.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BOT_Greg_v2_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ScheduleController : ControllerBase
{
    private readonly LiquipediaService _liquipedia;
    private readonly FilterService _filters;
    private readonly GuildsRepo _guilds;

    public ScheduleController(LiquipediaService liquipedia, FilterService filters, GuildsRepo guilds)
    {
        _liquipedia = liquipedia;
        _filters = filters;
        _guilds = guilds;
    }

    [HttpGet("matches")]
    public async Task<IActionResult> GetMatchesAsync([FromQuery] string guildDiscordId, [FromQuery] string status = "upcoming", [FromQuery] bool useFilters = true, [FromQuery] string? search = null)
    {
        bool live = string.Equals(status, "live", StringComparison.OrdinalIgnoreCase);

        if (!live && !string.Equals(status, "upcoming", StringComparison.OrdinalIgnoreCase))
            return BadRequest("status");

        var guild = await GetFilterGuildAsync(guildDiscordId, useFilters);
        if (useFilters && guild == null)
            return NotFound();

        var query = SearchQuery.Create(search);

        if (live)
        {
            var liveMatches = (await _liquipedia.GetLiveMatchesAsync())
                .Where(match => MatchFitsSearch(match, query));

            var filteredLiveMatches = await FilterMatchesAsync(liveMatches, guild);
            return Ok(filteredLiveMatches.OrderBy(match => match.DateTime).ToList());
        }

        var upcomingMatches = (await _liquipedia.GetMatchesAsync())
            .Where(match => MatchFitsSearch(match, query));

        var filteredUpcomingMatches = await FilterMatchesAsync(upcomingMatches, guild);
        return Ok(filteredUpcomingMatches.OrderBy(match => match.DateTime).ToList());
    }

    [HttpGet("results")]
    public async Task<IActionResult> GetResultsAsync([FromQuery] string guildDiscordId, [FromQuery] bool useFilters = true, [FromQuery] string? search = null)
    {
        var guild = await GetFilterGuildAsync(guildDiscordId, useFilters);
        if (useFilters && guild == null)
            return NotFound();

        var query = SearchQuery.Create(search);

        var results = (await _liquipedia.GetResultsAsync())
            .Where(match => MatchFitsSearch(match, query));

        var filteredResults = await FilterMatchesAsync(results, guild);
        return Ok(filteredResults.OrderByDescending(match => match.DateTime).ToList());
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEventsAsync([FromQuery] string guildDiscordId, [FromQuery] bool useFilters = true, [FromQuery] string? search = null)
    {
        var guild = await GetFilterGuildAsync(guildDiscordId, useFilters);
        if (useFilters && guild == null)
            return NotFound();

        var query = SearchQuery.Create(search);

        var events = (await _liquipedia.GetEventsAsync())
            .Where(ev => query == null || query.Fits(ev.EventName, ev.TierString, ev.LocationString))
            .Where(ev => guild == null || _filters.EventPassesFilters(guild, ev));

        // ongoing events first, events without a start date last
        return Ok(events
            .OrderByDescending(ev => ev.IsOngoing)
            .ThenBy(ev => ev.StartDate == null)
            .ThenBy(ev => ev.StartDate)
            .ToList());
    }

    // the guild is only needed if its filters are applied, null means "no filtering" (or "not found" if filters are requested)
    private async Task<Guild?> GetFilterGuildAsync(string guildDiscordId, bool useFilters)
    {
        if (!useFilters)
            return null;

        return await _guilds.GetGuildAsync(guildDiscordId);
    }

    private async Task<List<T>> FilterMatchesAsync<T>(IEnumerable<T> matches, Guild? guild) where T : MatchBase
    {
        if (guild == null || guild.Filters.Count == 0)
            return matches.ToList();

        // the teams are only needed for the vrs rank filter
        var teamsByName = guild.Filters.Any(filter => filter.VrsRank != null)
            ? await _filters.GetTeamsByNameAsync()
            : new Dictionary<string, Team>();

        return matches.Where(match => _filters.MatchPassesFilters(guild, match, teamsByName)).ToList();
    }

    private static bool MatchFitsSearch(MatchBase match, SearchQuery? query)
    {
        if (query == null)
            return true;

        return query.Fits(
            match.TeamX?.Name,
            match.TeamX?.NameShort,
            match.TeamY?.Name,
            match.TeamY?.NameShort,
            match.EventName,
            match.TierString,
            match.Stage);
    }
}
