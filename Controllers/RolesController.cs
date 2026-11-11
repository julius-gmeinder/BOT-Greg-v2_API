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
    public async Task<IActionResult> GetAllRoleIdsAsync([FromQuery] string guildId)
    {
        var guild = await _guilds.GetGuildAsync(guildId);
        if (guild == null)
            return Ok(new List<string>());

        return Ok(_roles.GetAllRoleIds(guild));
    }

    [HttpGet("default")]
    public async Task<IActionResult> GetDefaultRoleIdAsync([FromQuery] string guildId)
    {
        var guild = await _guilds.GetGuildAsync(guildId);
        if (guild?.AlertRoleId == null)
            return NotFound();

        return Ok(guild.AlertRoleId);
    }
}
