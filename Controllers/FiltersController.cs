using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Repos;
using Microsoft.AspNetCore.Mvc;

namespace BOT_Greg_v2_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FiltersController : ControllerBase
{
    private readonly FiltersRepo _filtersMatRes;
    private readonly GuildsRepo _guilds;

    public FiltersController(FiltersRepo filtersMatRes, GuildsRepo guilds)
    {
        _filtersMatRes = filtersMatRes;
        _guilds = guilds;
    }

    [HttpGet("")]
    public async Task<IActionResult> GetFiltersAsync([FromQuery] string guildId)
    {
        var guild = await _guilds.GetGuildAsync(guildId);
        if (guild == null)
            return NotFound();

        var filters = await _filtersMatRes.GetFiltersAsync(guildId);
        List<FilterDto> filterDtos = new();

        foreach (var filter in filters)
        {
            filterDtos.Add(new FilterDto()
            {
                Nr = filterDtos.Count + 1,
                RoleId = filter.RoleId,
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
    public async Task<IActionResult> GetFiltersRoleIdsAsync([FromQuery] string guildId)
    {
        var guild = await _guilds.GetGuildAsync(guildId);
        if (guild == null)
            return NotFound();

        var filters = await _filtersMatRes.GetFiltersAsync(guildId);
        var filterRoleIds = filters.Select(x => x.RoleId);

        return Ok(filterRoleIds);
    }

    // the properties of the filter are validated by the attributes of the dto
    [HttpPost("")]
    public async Task<IActionResult> AddFilterAsync([FromBody] FilterAddDto filterDto)
    {
        var guild = await _guilds.GetGuildAsync(filterDto.GuildId);
        if (guild == null)
            return NotFound();

        var filters = await _filtersMatRes.GetFiltersAsync(filterDto.GuildId);
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

        await _filtersMatRes.AddFilterAsync(guild, filterDto);
        return NoContent();
    }

    [HttpDelete("")]
    public async Task<IActionResult> RemoveFiltersAsync([FromBody] FilterDeleteDto filterDto)
    {
        var guild = await _guilds.GetGuildAsync(filterDto.GuildId);
        if (guild == null)
            return NotFound();

        var roleIdsToDelete = await _filtersMatRes.RemoveFiltersAsync(filterDto);
        return Ok(roleIdsToDelete);
    }
}
