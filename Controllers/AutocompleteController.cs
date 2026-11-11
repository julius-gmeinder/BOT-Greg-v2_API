using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Repos;
using BOT_Greg_v2_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace BOT_Greg_v2_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AutocompleteController : ControllerBase
{
    // discord shows a maximum of 25 autocomplete choices
    private const int MaxChoices = 25;

    private readonly LiquipediaService _liquipedia;
    private readonly AutocompleteRepo _ac;

    public AutocompleteController(LiquipediaService liquipedia, AutocompleteRepo ac)
    {
        _liquipedia = liquipedia;
        _ac = ac;
    }

    [HttpGet("teams")]
    public async Task<IActionResult> GetTeamsAsync([FromQuery] string? input = null)
    {
        // teams are already ordered by vrs rank
        var teams = (await _liquipedia.GetTeamsAsync())
            .Where(x => string.IsNullOrEmpty(input) || x.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
            .Take(MaxChoices);

        List<AutocompleteDto> autocompleteItems = new();
        foreach (var team in teams)
        {
            autocompleteItems.Add(new AutocompleteDto()
            {
                Name = $"{_ac.GetRankEmoji(team.Vrs?.Rank)}{team.Name}",
                Value = team.Name,
            });
        }

        return Ok(autocompleteItems);
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEventsAsync([FromQuery] string? input = null)
    {
        // events are already ordered by start date
        var events = (await _liquipedia.GetEventsAsync())
            .Where(x => string.IsNullOrEmpty(input) || x.EventName.Contains(input, StringComparison.OrdinalIgnoreCase))
            .Take(MaxChoices);

        List<AutocompleteDto> autocompleteItems = new();
        foreach (var eventItem in events)
        {
            autocompleteItems.Add(new AutocompleteDto()
            {
                Name = $"{_ac.GetTierEmoji(eventItem.Tier)}{eventItem.EventName}",
                Value = eventItem.EventName,
            });
        }

        return Ok(autocompleteItems);
    }

    [HttpGet("tiers")]
    public IActionResult GetTiers()
    {
        return Ok(_ac.GetTiers());
    }

    [HttpGet("vrs")]
    public IActionResult GetVrs()
    {
        return Ok(_ac.GetVrs());
    }
}
