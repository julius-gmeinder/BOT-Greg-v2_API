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
        var guild = await _guilds.GetGuildAsync(linkDto.GuildDiscordId);
        if (guild == null)
            return NotFound();

        // without role management the bot has no roles that could be linked
        // the other endpoints need no check: disabling the role management deletes all links, so they just return nothing
        if (!guild.Settings.RoleManagement)
            return Conflict("roleManagement");

        if (!_roles.RoleExists(guild, linkDto.BotRoleDiscordId))
            return NotFound();

        var links = await _links.GetRoleLinksAsync(linkDto.GuildDiscordId);
        if (links.Count >= Constants.MaxRoleLinksPerGuild)
            return Conflict("limit");

        if (links.Any(x => x.BotRoleDiscordId == linkDto.BotRoleDiscordId && x.LinkedRoleDiscordId == linkDto.LinkedRoleDiscordId))
            return Conflict("duplicate");

        await _links.LinkRolesAsync(guild, linkDto);
        return NoContent();
    }

    [HttpDelete("")]
    public async Task<IActionResult> RemoveRoleLinkAsync([FromBody] RoleLinkRemoveDto linkDto)
    {
        var link = await _links.GetRoleLinkAsync(linkDto.BotRoleDiscordId, linkDto.LinkedRoleDiscordId);
        if (link == null)
            return NotFound();

        await _links.UnlinkRolesAsync(link);
        return NoContent();
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetRoleLinksTreeAsync([FromQuery] string guildDiscordId)
    {
        var guild = await _guilds.GetGuildAsync(guildDiscordId);
        if (guild == null)
            return NotFound();

        return Ok(_links.GetRoleLinksTree(guild));
    }

    [HttpGet("bot")]
    public async Task<IActionResult> GetLinkedBotRoleDiscordIdsAsync([FromQuery] string guildDiscordId)
    {
        var guild = await _guilds.GetGuildAsync(guildDiscordId);
        if (guild == null)
            return NotFound();

        return Ok(_links.GetDistinctLinkedBotRoleDiscordIds(guild));
    }

    [HttpGet("linked")]
    public async Task<IActionResult> GetLinkedRoleDiscordIdsAsync([FromQuery] string botRoleDiscordId)
    {
        var linkedRoleDiscordIds = await _links.GetLinkedRoleDiscordIdsAsync(botRoleDiscordId);
        return Ok(linkedRoleDiscordIds);
    }
}
