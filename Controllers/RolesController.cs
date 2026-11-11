using BOT_Greg_v2_API.Repos;
using Microsoft.AspNetCore.Mvc;

namespace BOT_Greg_v2_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly RolesRepo _roles;
    private readonly GuildsRepo _guilds;

    public RolesController(RolesRepo roles, GuildsRepo guilds)
    {
        _roles = roles;
        _guilds = guilds;
    }

    [HttpGet("")]
    public async Task<IActionResult> GetAllRoleDiscordIdsAsync([FromQuery] string guildDiscordId)
    {
        var guild = await _guilds.GetGuildAsync(guildDiscordId);
        if (guild == null)
            return Ok(new List<string>());

        return Ok(_roles.GetAllRoleDiscordIds(guild));
    }

    [HttpGet("default")]
    public async Task<IActionResult> GetDefaultRoleDiscordIdAsync([FromQuery] string guildDiscordId)
    {
        var guild = await _guilds.GetGuildAsync(guildDiscordId);
        if (guild?.AlertRoleDiscordId == null)
            return NotFound();

        return Ok(guild.AlertRoleDiscordId);
    }
}
