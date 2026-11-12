using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Repos;
using Microsoft.AspNetCore.Mvc;

namespace BOT_Greg_v2_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FiltersController : ControllerBase
{
    private readonly FiltersRepo _filters;
    private readonly GuildsRepo _guilds;

    public FiltersController(FiltersRepo filters, GuildsRepo guilds)
    {
        _filters = filters;
        _guilds = guilds;
    }

    [HttpGet("")]
    public async Task<IActionResult> GetFiltersAsync([FromQuery] string guildDiscordId)
    {
        var guild = await _guilds.GetGuildAsync(guildDiscordId);
        if (guild == null)
            return NotFound();

        var filters = await _filters.GetFiltersAsync(guildDiscordId);
        List<FilterDto> filterDtos = new();

        foreach (var filter in filters)
        {
            filterDtos.Add(new FilterDto()
            {
                Id = filter.Id,
                Nr = filterDtos.Count + 1,
                RoleDiscordId = filter.RoleDiscordId,
                Team = filter.Team,
                Event = filter.Event,
                EventTier = filter.EventTier,
                VrsRank = filter.VrsRank,
                HltvCoverage = filter.HltvCoverage
            });
        }

        return Ok(filterDtos);
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetFiltersRoleDiscordIdsAsync([FromQuery] string guildDiscordId)
    {
        var guild = await _guilds.GetGuildAsync(guildDiscordId);
        if (guild == null)
            return NotFound();

        var filters = await _filters.GetFiltersAsync(guildDiscordId);

        // filters only have a role if the role management is enabled, so the list is empty if it is disabled
        var filterRoleDiscordIds = filters.Where(x => x.RoleDiscordId != null).Select(x => x.RoleDiscordId);

        return Ok(filterRoleDiscordIds);
    }

    // the properties of the filter are validated by the attributes of the dto
    [HttpPost("")]
    public async Task<IActionResult> AddFilterAsync([FromBody] FilterAddDto filterDto)
    {
        var guild = await _guilds.GetGuildAsync(filterDto.GuildDiscordId);
        if (guild == null)
            return NotFound();

        // the bot only creates a role for the filter if the role management is enabled
        if (guild.Settings.RoleManagement && filterDto.RoleDiscordId == null)
            return BadRequest("roleRequired");

        if (!guild.Settings.RoleManagement && filterDto.RoleDiscordId != null)
            return Conflict("roleManagement");

        var filters = await _filters.GetFiltersAsync(filterDto.GuildDiscordId);
        if (filters.Count >= Constants.MaxFiltersPerGuild)
            return Conflict("limit");

        bool isDuplicate = filters.Any(x =>
            x.Team == filterDto.Team &&
            x.Event == filterDto.Event &&
            x.EventTier == filterDto.EventTier &&
            x.VrsRank == filterDto.VrsRank &&
            x.HltvCoverage == filterDto.HltvCoverage);

        if (isDuplicate)
            return Conflict("duplicate");

        await _filters.AddFilterAsync(guild, filterDto);
        return NoContent();
    }

    // returns the roles of the deleted filters, the bot has to delete them in discord (empty if the role management is disabled)
    [HttpDelete("")]
    public async Task<IActionResult> RemoveFiltersAsync([FromBody] FilterDeleteDto filterDto)
    {
        var guild = await _guilds.GetGuildAsync(filterDto.GuildDiscordId);
        if (guild == null)
            return NotFound();

        var roleDiscordIdsToDelete = await _filters.RemoveFiltersAsync(filterDto);
        return Ok(roleDiscordIdsToDelete);
    }
}
