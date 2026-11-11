using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Repos;
using Microsoft.AspNetCore.Mvc;

namespace BOT_Greg_v2_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RoleLinksController : ControllerBase
{
    private readonly RoleLinksRepo _links;
    private readonly RolesRepo _roles;
    private readonly GuildsRepo _guilds;

    public RoleLinksController(RoleLinksRepo links, RolesRepo roles, GuildsRepo guilds)
    {
        _links = links;
        _roles = roles;
        _guilds = guilds;
    }

    [HttpPost("")]
    public async Task<IActionResult> AddRoleLinkAsync([FromBody] RoleLinkAddDto linkDto)
    {
        var guild = await _guilds.GetGuildAsync(linkDto.GuildId);
        if (guild == null)
            return NotFound();

        if (!_roles.RoleExists(guild, linkDto.BotRoleId))
            return NotFound();

        var links = await _links.GetRoleLinksAsync(linkDto.GuildId);
        if (links.Count >= Constants.MaxRoleLinksPerGuild)
            return Conflict("limit");

        if (links.Any(x => x.BotRoleId == linkDto.BotRoleId && x.LinkedRoleId == linkDto.LinkedRoleId))
            return Conflict("duplicate");

        await _links.LinkRolesAsync(guild, linkDto);
        return NoContent();
    }

    [HttpDelete("")]
    public async Task<IActionResult> RemoveRoleLinkAsync([FromBody] RoleLinkRemoveDto linkDto)
    {
        var link = await _links.GetRoleLinkAsync(linkDto.BotRoleId, linkDto.LinkedRoleId);
        if (link == null)
            return NotFound();

        await _links.UnlinkRolesAsync(link);
        return NoContent();
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetRoleLinksTreeAsync([FromQuery] string guildId)
    {
        var guild = await _guilds.GetGuildAsync(guildId);
        if (guild == null)
            return NotFound();

        return Ok(_links.GetRoleLinksTree(guild));
    }

    [HttpGet("bot")]
    public async Task<IActionResult> GetLinkedBotRoleIdsAsync([FromQuery] string guildId)
    {
        var guild = await _guilds.GetGuildAsync(guildId);
        if (guild == null)
            return NotFound();

        return Ok(_links.GetDistinctLinkedBotRoleIds(guild));
    }

    [HttpGet("linked")]
    public async Task<IActionResult> GetLinkedRoleIdsAsync([FromQuery] string botRoleId)
    {
        var linkedRoleIds = await _links.GetLinkedRoleIdsAsync(botRoleId);
        return Ok(linkedRoleIds);
    }
}
